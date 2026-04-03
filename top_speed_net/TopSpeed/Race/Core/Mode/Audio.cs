using System;
using System.Numerics;
using System.Threading;
using TopSpeed.Audio;
using TS.Audio;

namespace TopSpeed.Race
{
    internal abstract partial class RaceMode
    {
        protected void Speak(AudioSourceHandle sound, bool unKey = false)
        {
            if (sound == null)
                return;

            var length = Math.Max(0.05f, sound.GetLengthSeconds());
            _speakTime = Math.Max(_speakTime, _elapsedTotal) + length;
            QueueSound(sound);

            if (unKey)
            {
                _unkeyQueue++;
                PushEvent(Events.RaceEventType.PlayRadioSound, length);
            }
        }

        protected void SpeakText(string text)
        {
            _speech.Speak(text);
        }

        protected void UpdateAudioListener(float elapsed)
        {
            var driverOffsetX = -_car.WidthM * 0.25f;
            var driverOffsetZ = _car.LengthM * 0.1f;
            var worldPosition = new Vector3(_car.PositionX + driverOffsetX, 0f, _car.PositionY + driverOffsetZ);

            var worldVelocity = Vector3.Zero;
            if (_listenerInitialized && elapsed > 0f)
            {
                worldVelocity = (worldPosition - _lastListenerPosition) / elapsed;
            }
            _lastListenerPosition = worldPosition;
            _listenerInitialized = true;

            var forward = new Vector3(0f, 0f, 1f);
            var up = new Vector3(0f, 1f, 0f);
            var position = AudioWorld.ToMeters(worldPosition);
            var velocity = AudioWorld.ToMeters(worldVelocity);
            _audio.UpdateListener(position, forward, up, velocity);
            _localRadio.UpdateSpatial(worldPosition.X, worldPosition.Z, worldVelocity);
        }

        protected float GetRelativeTrackDelta(float otherPositionY)
        {
            return otherPositionY - _car.PositionY;
        }

        protected void FlushPendingSounds()
        {
            for (var i = _events.Count - 1; i >= 0; i--)
            {
                if (_events[i].Sound != null)
                {
                    _events.RemoveAt(i);
                }
            }
            _soundQueue.Clear();
        }

        protected void FadeIn()
        {
            if (_soundTheme4 == null)
                return;
            var target = (int)Math.Round(_settings.MusicVolume * 100f);
            var volume = 0;
            _soundTheme4.SetVolumePercent(volume);
            for (var i = 0; i < 10; i++)
            {
                volume = Math.Min(target, volume + Math.Max(1, target / 10));
                _soundTheme4.SetVolumePercent(volume);
                Thread.Sleep(25);
            }
        }

        protected void FadeOut()
        {
            if (_soundTheme4 == null)
                return;
            var volume = (int)Math.Round(_settings.MusicVolume * 100f);
            for (var i = 0; i < 10; i++)
            {
                volume = Math.Max(0, volume - Math.Max(1, volume / 10));
                _soundTheme4.SetVolumePercent(volume);
                Thread.Sleep(25);
            }
        }

        protected void QueueSound(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            _soundQueue.Enqueue(sound);
        }

        private void RefreshCategoryVolumes()
        {
            var ambientPercent = _settings.AudioVolumes?.AmbientsAndSourcesPercent ?? 100;
            _track.SetAmbientVolumePercent(ambientPercent);
        }
    }
}

