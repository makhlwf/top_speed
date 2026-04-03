using System;
using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        private enum InputScope
        {
            Driving,
            Auxiliary
        }

        private enum TriggerMode
        {
            Hold,
            Press
        }

        private readonly struct InputActionMeta
        {
            public InputActionMeta(InputScope scope, TriggerMode keyboardMode, TriggerMode controllerMode, bool allowNumpadEnterAlias = false)
            {
                Scope = scope;
                KeyboardMode = keyboardMode;
                ControllerMode = controllerMode;
                AllowNumpadEnterAlias = allowNumpadEnterAlias;
            }

            public InputScope Scope { get; }
            public TriggerMode KeyboardMode { get; }
            public TriggerMode ControllerMode { get; }
            public bool AllowNumpadEnterAlias { get; }
        }

        private readonly struct InputActionBinding
        {
            public InputActionBinding(
                string label,
                InputActionMeta meta,
                Func<Key> getKey,
                Action<Key> setKey,
                Func<AxisOrButton> getAxis,
                Action<AxisOrButton> setAxis)
            {
                Label = label;
                Meta = meta;
                GetKey = getKey;
                SetKey = setKey;
                GetAxis = getAxis;
                SetAxis = setAxis;
            }

            public string Label { get; }
            public InputActionMeta Meta { get; }
            public Func<Key> GetKey { get; }
            public Action<Key> SetKey { get; }
            public Func<AxisOrButton> GetAxis { get; }
            public Action<AxisOrButton> SetAxis { get; }
        }

        private readonly RaceSettings _settings;
        private readonly InputState _lastState;
        private readonly InputState _prevState;
        private readonly List<InputActionDefinition> _actionDefinitions;
        private readonly Dictionary<InputAction, InputActionBinding> _actionBindings;
        private AxisOrButton _left;
        private AxisOrButton _right;
        private AxisOrButton _throttle;
        private AxisOrButton _brake;
        private AxisOrButton _clutch;
        private AxisOrButton _gearUp;
        private AxisOrButton _gearDown;
        private AxisOrButton _horn;
        private AxisOrButton _requestInfo;
        private AxisOrButton _currentGear;
        private AxisOrButton _currentLapNr;
        private AxisOrButton _currentRacePerc;
        private AxisOrButton _currentLapPerc;
        private AxisOrButton _currentRaceTime;
        private AxisOrButton _startEngine;
        private AxisOrButton _reportDistance;
        private AxisOrButton _reportSpeed;
        private AxisOrButton _trackName;
        private AxisOrButton _pause;
        private InputDeviceMode _deviceMode;
        private Key _kbLeft;
        private Key _kbRight;
        private Key _kbThrottle;
        private Key _kbBrake;
        private Key _kbClutch;
        private Key _kbGearUp;
        private Key _kbGearDown;
        private Key _kbHorn;
        private Key _kbRequestInfo;
        private Key _kbCurrentGear;
        private Key _kbCurrentLapNr;
        private Key _kbCurrentRacePerc;
        private Key _kbCurrentLapPerc;
        private Key _kbCurrentRaceTime;
        private Key _kbStartEngine;
        private Key _kbReportDistance;
        private Key _kbReportSpeed;
        private Key _kbPlayer1;
        private Key _kbPlayer2;
        private Key _kbPlayer3;
        private Key _kbPlayer4;
        private Key _kbPlayer5;
        private Key _kbPlayer6;
        private Key _kbPlayer7;
        private Key _kbPlayer8;
        private Key _kbTrackName;
        private Key _kbPlayerNumber;
        private Key _kbPause;
        private Key _kbPlayerPos1;
        private Key _kbPlayerPos2;
        private Key _kbPlayerPos3;
        private Key _kbPlayerPos4;
        private Key _kbPlayerPos5;
        private Key _kbPlayerPos6;
        private Key _kbPlayerPos7;
        private Key _kbPlayerPos8;
        private Key _kbFlush;
        private State _center;
        private State _lastController;
        private State _prevController;
        private bool _hasCenter;
        private bool _hasPrevController;
        private bool _controllerAvailable;
        private bool _allowDrivingInput;
        private bool _allowAuxiliaryInput;
        private bool _overlayInputBlocked;
        private bool _controllerIsRacingWheel;
        private bool _hasPedalBaseline;
        private State _pedalBaseline;
        private float _simThrottle;
        private float _simBrake;
        private float _simSteer;
        private float _simClutch;
        private bool UseController => _deviceMode != InputDeviceMode.Keyboard && _controllerAvailable;
        private bool UseKeyboard => _deviceMode != InputDeviceMode.Controller || !_controllerAvailable;

        public KeyMapManager KeyMap { get; }

        public RaceInput(RaceSettings settings)
        {
            _settings = settings;
            _lastState = new InputState();
            _prevState = new InputState();
            _actionDefinitions = new List<InputActionDefinition>();
            _actionBindings = CreateActionBindings();
            Initialize();
            KeyMap = new KeyMapManager(this);
        }
    }
}


