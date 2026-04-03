using System;
using System.Collections.Generic;
using System.Globalization;
using SharpDX;
using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Vibration;
using TopSpeed.Localization;
using DirectInputJoystick = SharpDX.DirectInput.Joystick;

namespace TopSpeed.Input.Devices.Controller
{
    internal sealed class Device : IVibrationDevice, IAdvancedVibrationDevice
    {
        private readonly DirectInputJoystick? _controller;
        private readonly Guid _instanceGuid;
        private readonly string _displayName;
        private readonly bool _isRacingWheel;
        private State _state;
        private readonly Dictionary<VibrationEffectType, ForceFeedbackEffect> _effects = new Dictionary<VibrationEffectType, ForceFeedbackEffect>();
        private bool _connected;

        public Device(DirectInput directInput, IntPtr windowHandle, Choice choice)
        {
            if (choice == null)
                throw new ArgumentNullException(nameof(choice));

            _instanceGuid = choice.InstanceGuid;
            _displayName = choice.DisplayName;
            _isRacingWheel = choice.IsRacingWheel;

            if (_instanceGuid == Guid.Empty)
                return;

            _controller = new DirectInputJoystick(directInput, _instanceGuid);
            _controller.SetCooperativeLevel(windowHandle, CooperativeLevel.Exclusive | CooperativeLevel.Background);
            _controller.Properties.BufferSize = 128;

            foreach (var deviceObject in _controller.GetObjects())
            {
                if ((deviceObject.ObjectId.Flags & DeviceObjectTypeFlags.Axis) != 0)
                {
                    _controller.GetObjectPropertiesById(deviceObject.ObjectId).Range = new InputRange(-100, 100);
                }
            }

            try
            {
                _controller.Properties.AutoCenter = false;
            }
            catch (SharpDXException)
            {
                // Some devices do not support auto-centering configuration.
            }

            _connected = true;
        }

        public bool IsAvailable => _controller != null && _connected;

        public Guid InstanceGuid => _instanceGuid;
        public string DisplayName => _displayName;
        public bool IsRacingWheel => _isRacingWheel;

        internal DirectInputJoystick? Native => _controller;

        public bool ForceFeedbackCapable
        {
            get
            {
                if (_controller == null)
                    return false;
                return (_controller.Capabilities.Flags & DeviceFlags.ForceFeedback) != 0;
            }
        }

        public State State => _state;

        public bool Update()
        {
            if (_controller == null)
                return false;
            try
            {
                _controller.Acquire();
                _controller.Poll();
                var state = _controller.GetCurrentState();
                _state = State.From(state);
                _connected = true;
                return true;
            }
            catch (SharpDXException)
            {
                _connected = false;
                return false;
            }
        }

        public VibrationFeatureAvailability GetFeatureAvailability(VibrationFeature feature)
        {
            if (feature != VibrationFeature.EffectFilePlayback)
                return VibrationFeatureAvailability.Unsupported;

            if (!IsAvailable)
                return VibrationFeatureAvailability.Unavailable;

            return ForceFeedbackCapable
                ? VibrationFeatureAvailability.Supported
                : VibrationFeatureAvailability.Unsupported;
        }

        public VibrationOperationResult LoadEffect(VibrationEffectType type, string effectPath)
        {
            if (string.IsNullOrWhiteSpace(effectPath))
                return VibrationOperationResult.InvalidInput;

            var availability = GetFeatureAvailability(VibrationFeature.EffectFilePlayback);
            if (availability == VibrationFeatureAvailability.Unavailable)
                return VibrationOperationResult.Unavailable;
            if (availability != VibrationFeatureAvailability.Supported)
                return VibrationOperationResult.Unsupported;

            if (_effects.TryGetValue(type, out var existing))
            {
                existing.Dispose();
                _effects.Remove(type);
            }

            var effect = new ForceFeedbackEffect(this, effectPath);
            if (!effect.HasLoadedEffects)
            {
                effect.Dispose();
                return VibrationOperationResult.Failed;
            }

            _effects[type] = effect;
            return VibrationOperationResult.Success;
        }

        public void PlayEffect(VibrationEffectType type, int intensity = 10000)
        {
            if (_effects.TryGetValue(type, out var effect))
                effect.Play();
        }

        public void StopEffect(VibrationEffectType type)
        {
            if (_effects.TryGetValue(type, out var effect))
                effect.Stop();
        }

        public void Gain(VibrationEffectType type, int value)
        {
            if (_effects.TryGetValue(type, out var effect))
                effect.Gain(value);
        }

        public void Dispose()
        {
            if (_controller == null)
                return;
            foreach (var effect in _effects.Values)
                effect.Dispose();
            _effects.Clear();
            try
            {
                _controller.Unacquire();
            }
            catch (SharpDXException)
            {
            }
            _controller.Dispose();
        }

        public static List<Choice> Discover(DirectInput directInput)
        {
            var discovered = new Dictionary<Guid, Choice>();
            AddDeviceType(discovered, directInput, DeviceType.Driving, treatAsWheel: true);
            AddDeviceType(discovered, directInput, DeviceType.Joystick, treatAsWheel: false);
            AddDeviceType(discovered, directInput, DeviceType.Gamepad, treatAsWheel: false);

            var result = new List<Choice>(discovered.Values);
            result.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.CurrentCultureIgnoreCase));
            return result;
        }

        private static void AddDeviceType(
            Dictionary<Guid, Choice> discovered,
            DirectInput directInput,
            DeviceType deviceType,
            bool treatAsWheel)
        {
            var devices = directInput.GetDevices(deviceType, DeviceEnumerationFlags.AttachedOnly);
            for (var i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                var instanceGuid = device.InstanceGuid;
                if (instanceGuid == Guid.Empty)
                    continue;

                var displayName = BuildDisplayName(device);
                var wheelByName = LooksLikeWheel(displayName) || LooksLikeWheel(device.ProductName);
                var isWheel = treatAsWheel || wheelByName;

                if (discovered.TryGetValue(instanceGuid, out var existing))
                {
                    if (!existing.IsRacingWheel && isWheel)
                        discovered[instanceGuid] = new Choice(existing.InstanceGuid, existing.DisplayName, true);
                    continue;
                }

                discovered.Add(instanceGuid, new Choice(instanceGuid, displayName, isWheel));
            }
        }

        private static string BuildDisplayName(DeviceInstance device)
        {
            if (!string.IsNullOrWhiteSpace(device.InstanceName))
                return device.InstanceName.Trim();
            if (!string.IsNullOrWhiteSpace(device.ProductName))
                return device.ProductName.Trim();
            return LocalizationService.Format(
                LocalizationService.Mark("Controller {0}"),
                device.InstanceGuid.ToString("D", CultureInfo.InvariantCulture));
        }

        private static bool LooksLikeWheel(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var value = (name ?? string.Empty).ToLowerInvariant();
            return value.Contains("wheel")
                || value.Contains("steering")
                || value.Contains("pedal")
                || value.Contains("racing");
        }
    }
}

