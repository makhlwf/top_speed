using System;
using SharpDX.DirectInput;
using TopSpeed.Speech;
using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class InputMappingHandler
    {
        private readonly InputManager _input;
        private readonly RaceInput _raceInput;
        private readonly RaceSettings _settings;
        private readonly SpeechService _speech;
        private readonly Action _saveSettings;

        private bool _mappingActive;
        private InputMappingMode _mappingMode;
        private InputAction _mappingAction;
        private bool _mappingNeedsInstruction;
        private JoystickStateSnapshot _mappingPrevJoystick;
        private bool _mappingHasPrevJoystick;

        public InputMappingHandler(
            InputManager input,
            RaceInput raceInput,
            RaceSettings settings,
            SpeechService speech,
            Action saveSettings)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _raceInput = raceInput ?? throw new ArgumentNullException(nameof(raceInput));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _speech = speech ?? throw new ArgumentNullException(nameof(speech));
            _saveSettings = saveSettings ?? throw new ArgumentNullException(nameof(saveSettings));
        }

        public bool IsActive => _mappingActive;

        public void BeginMapping(InputMappingMode mode, InputAction action)
        {
            if (mode == InputMappingMode.Joystick)
            {
                if (_input.VibrationDevice == null || !_input.VibrationDevice.IsAvailable)
                {
                    _speech.Speak("No joystick detected.");
                    return;
                }
            }

            _mappingActive = true;
            _mappingMode = mode;
            _mappingAction = action;
            _mappingHasPrevJoystick = false;
            _mappingNeedsInstruction = true;
        }

        public void Update()
        {
            if (!_mappingActive)
                return;

            if (_mappingNeedsInstruction)
            {
                _mappingNeedsInstruction = false;
                var instruction = _raceInput.KeyMap.GetMappingInstruction(_mappingMode == InputMappingMode.Keyboard, _mappingAction);
                _speech.Speak(instruction);
            }

            if (_input.WasPressed(Key.Escape))
            {
                _mappingActive = false;
                _speech.Speak("Mapping cancelled.");
                return;
            }

            if (_mappingMode == InputMappingMode.Keyboard)
                TryCaptureKeyboardMapping();
            else
                TryCaptureJoystickMapping();
        }

        public string FormatMappingValue(InputAction action, InputMappingMode mode)
        {
            return mode == InputMappingMode.Keyboard
                ? KeyMapManager.FormatKey(_raceInput.KeyMap.GetKey(action))
                : KeyMapManager.FormatAxis(_raceInput.KeyMap.GetAxis(action));
        }
    }
}
