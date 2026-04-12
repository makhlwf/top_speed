using System;
using System.Collections.Generic;
using MiniAudioEx.Core.AdvancedAPI;
using MiniAudioEx.Native;

namespace TS.Audio
{
    public sealed partial class AudioBus
    {
        public void SetEffectsEnabled(bool enabled)
        {
            lock (_effectLock)
            {
                _effectsEnabled = enabled;
                RebuildEffectChain();
            }

            _output.Diagnostics.Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.BusEffectsEnabledChanged,
                AudioDiagnosticEntityType.Bus,
                _output.Name,
                Name,
                null,
                enabled ? "Audio bus effects enabled." : "Audio bus effects disabled.",
                new Dictionary<string, object?>
                {
                    ["effectsEnabled"] = enabled,
                    ["effectCount"] = _effects.Count
                },
                new AudioDiagnosticSnapshot(bus: CaptureSnapshot()));
        }

        public BusEffect AddEffect(AudioEffectProcessCallback process, string? name = null)
        {
            return InsertEffect(int.MaxValue, process, name);
        }

        public BusEffect InsertEffect(int index, AudioEffectProcessCallback process, string? name = null)
        {
            ThrowIfDisposed();
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            lock (_effectLock)
            {
                var node = new MaEffectNode();
                var init = node.Initialize(_output.Runtime.EngineHandle, (uint)_output.SampleRate, (uint)_output.Channels);
                if (init != ma_result.success)
                {
                    node.Dispose();
                    throw new InvalidOperationException("Failed to initialize bus effect node: " + init);
                }

                var effect = new BusEffect(this, node, process, name);
                node.Process += (MaEffectNode _, NativeArray<float> framesIn, uint frameCountIn, NativeArray<float> framesOut, ref uint frameCountOut, uint channels) =>
                {
                    if (effect.IsDisposed || !effect.Enabled)
                    {
                        framesIn.CopyTo(framesOut);
                        frameCountOut = frameCountIn;
                        return;
                    }

                    process(framesIn, frameCountIn, framesOut, ref frameCountOut, channels);
                };

                var insertAt = index < 0 ? 0 : Math.Min(index, _effects.Count);
                _effects.Insert(insertAt, effect);
                RebuildEffectChain();
                _output.Diagnostics.Emit(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.BusEffectAdded,
                    AudioDiagnosticEntityType.Bus,
                    _output.Name,
                    Name,
                    null,
                    "Audio bus effect added.",
                    new Dictionary<string, object?>
                    {
                        ["effectName"] = effect.Name,
                        ["index"] = insertAt,
                        ["effectCount"] = _effects.Count
                    },
                    new AudioDiagnosticSnapshot(bus: CaptureSnapshot()));
                return effect;
            }
        }

        public bool MoveEffect(int fromIndex, int toIndex)
        {
            lock (_effectLock)
            {
                if (fromIndex < 0 || fromIndex >= _effects.Count || toIndex < 0 || toIndex >= _effects.Count || fromIndex == toIndex)
                    return false;

                var effect = _effects[fromIndex];
                _effects.RemoveAt(fromIndex);
                _effects.Insert(toIndex, effect);
                RebuildEffectChain();
                _output.Diagnostics.Emit(
                    AudioDiagnosticLevel.Trace,
                    AudioDiagnosticKind.BusEffectMoved,
                    AudioDiagnosticEntityType.Bus,
                    _output.Name,
                    Name,
                    null,
                    "Audio bus effect moved.",
                    new Dictionary<string, object?>
                    {
                        ["fromIndex"] = fromIndex,
                        ["toIndex"] = toIndex,
                        ["effectName"] = effect.Name
                    },
                    new AudioDiagnosticSnapshot(bus: CaptureSnapshot()));
                return true;
            }
        }

        public bool RemoveEffectAt(int index)
        {
            BusEffect? removed;
            lock (_effectLock)
            {
                if (index < 0 || index >= _effects.Count)
                    return false;

                removed = _effects[index];
                _effects.RemoveAt(index);
                RebuildEffectChain();
            }

            DetachEffect(removed);
            _output.Diagnostics.Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.BusEffectRemoved,
                AudioDiagnosticEntityType.Bus,
                _output.Name,
                Name,
                null,
                "Audio bus effect removed.",
                new Dictionary<string, object?>
                {
                    ["index"] = index,
                    ["effectName"] = removed.Name,
                    ["effectCount"] = _effects.Count
                },
                new AudioDiagnosticSnapshot(bus: CaptureSnapshot()));
            return true;
        }

        public IReadOnlyList<BusEffect> GetEffects()
        {
            lock (_effectLock)
                return new List<BusEffect>(_effects).AsReadOnly();
        }

        public void ClearEffects()
        {
            BusEffect[] removed;
            lock (_effectLock)
            {
                if (_effects.Count == 0)
                    return;

                removed = _effects.ToArray();
                _effects.Clear();
                RebuildEffectChain();
            }

            for (var i = 0; i < removed.Length; i++)
                DetachEffect(removed[i]);

            _output.Diagnostics.Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.BusEffectsCleared,
                AudioDiagnosticEntityType.Bus,
                _output.Name,
                Name,
                null,
                "Audio bus effects cleared.",
                new Dictionary<string, object?>
                {
                    ["removedCount"] = removed.Length
                },
                new AudioDiagnosticSnapshot(bus: CaptureSnapshot()));
        }

        internal void RemoveEffect(BusEffect effect)
        {
            if (effect == null)
                return;

            var removed = false;
            lock (_effectLock)
            {
                removed = _effects.Remove(effect);
                if (removed)
                    RebuildEffectChain();
            }

            if (!removed)
                return;

            DetachEffect(effect);
        }

        private void RebuildEffectChain()
        {
            MiniAudioNative.ma_node_detach_all_output_buses(NodeHandle);

            var target = _parent?.NodeHandle ?? _output.Runtime.Endpoint;
            if (!_effectsEnabled || _effects.Count == 0)
            {
                MiniAudioNative.ma_node_attach_output_bus(NodeHandle, 0, target, 0);
                return;
            }

            for (var i = 0; i < _effects.Count; i++)
                _effects[i].Node.DetachAllOutputBuses();

            MiniAudioNative.ma_node_attach_output_bus(NodeHandle, 0, _effects[0].Node.NodeHandle, 0);
            for (var i = 0; i < _effects.Count; i++)
            {
                var next = i == _effects.Count - 1 ? target : _effects[i + 1].Node.NodeHandle;
                _effects[i].Node.AttachOutputBus(0, next, 0);
            }
        }

        private void DetachEffect(BusEffect effect)
        {
            effect.Node.DetachAllOutputBuses();
            effect.MarkDetached();
            _output.RetireEffect(effect);
        }
    }
}
