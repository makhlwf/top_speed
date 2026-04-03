using System;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Vibration;
using ControllerDevice = TopSpeed.Input.Devices.Controller.Device;

namespace TopSpeed.Input.Devices.Vibration
{
    internal sealed class ForceFeedbackEffect : IDisposable
    {
        private readonly List<Effect> _effects = new List<Effect>();
        public bool HasLoadedEffects => _effects.Count > 0;

        public ForceFeedbackEffect(ControllerDevice controller, string effectPath)
        {
            if (controller == null)
                return;
            var device = controller.Native;
            if (device == null)
                return;
            if (!File.Exists(effectPath))
                return;

            try
            {
                var effects = device.GetEffectsInFile(effectPath, EffectFileFlags.ModidyIfNeeded);
                foreach (var effectFile in effects)
                {
                    var effect = new Effect(device, effectFile.Guid, effectFile.Parameters);
                    _effects.Add(effect);
                    try
                    {
                        effect.Download();
                    }
                    catch (SharpDXException)
                    {
                    }
                }
            }
            catch (SharpDXException)
            {
            }
        }

        public void Play()
        {
            foreach (var effect in _effects)
            {
                try
                {
                    effect.Start(0, EffectPlayFlags.None);
                }
                catch (SharpDXException)
                {
                }
            }
        }

        public void Stop()
        {
            foreach (var effect in _effects)
            {
                try
                {
                    effect.Stop();
                }
                catch (SharpDXException)
                {
                }
            }
        }

        public void Gain(int value)
        {
            var gain = value;
            if (gain > 10000)
                gain = 10000;
            if (gain < 0)
                gain = 0;

            foreach (var effect in _effects)
            {
                try
                {
                    var parameters = effect.GetParameters(EffectParameterFlags.Gain);
                    parameters.Gain = gain;
                    effect.SetParameters(parameters, EffectParameterFlags.Gain);
                }
                catch (SharpDXException)
                {
                }
            }
        }

        public void Dispose()
        {
            foreach (var effect in _effects)
                effect.Dispose();
            _effects.Clear();
        }
    }
}

