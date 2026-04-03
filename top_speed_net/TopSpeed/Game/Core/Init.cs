using System;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Core.Multiplayer;
using TopSpeed.Core.Settings;
using TopSpeed.Core.Updates;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Runtime;
using TopSpeed.Shortcuts;
using TopSpeed.Speech;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        public Game(IWindowHost window, ITextInputService textInput, IFileDialogs fileDialogs)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _textInput = textInput ?? throw new ArgumentNullException(nameof(textInput));
            _fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
            _settingsManager = new SettingsManager();
            var settingsLoad = _settingsManager.Load();
            _settings = settingsLoad.Settings;
            _settingsIssues = settingsLoad.Issues;
            _settingsFileMissing = settingsLoad.SettingsFileMissing;
            _clientLanguages = ClientLanguages.Load();
            _settings.Language = ClientLanguages.ResolveCode(_settings.Language, _clientLanguages);
            LocalizationBootstrap.Configure(_settings.Language, LocalizationBootstrap.ClientCatalogGroup);
            var audio = new AudioManager(_settings.HrtfAudio, _settings.AutoDetectAudioDeviceFormat);
            var backendRegistry = new BackendRegistry(
                new IKeyboardBackendFactory[]
                {
#if NETFRAMEWORK
                    new TopSpeed.Input.Devices.Keyboard.Backends.DirectInput.Factory(),
                    new TopSpeed.Input.Devices.Keyboard.Backends.Sdl.Factory()
#else
                    new TopSpeed.Input.Devices.Keyboard.Backends.Eto.Factory()
#endif
                },
                new IControllerBackendFactory[]
                {
                    new TopSpeed.Input.Backends.DirectInput.Factory(),
                    new TopSpeed.Input.Backends.Sdl.Factory()
                });
            var input = new InputService(_window.NativeHandle, backendRegistry, _window as IKeyboardEventSource);
            var speech = new SpeechService(input.IsAnyInputHeld, input.PrepareForInterruptableSpeech);
            _audio = audio;
            _input = input;
            _speech = speech;
            speech.ScreenReaderRateMs = _settings.ScreenReaderRateMs;
            input.ControllerScanTimedOut += () => speech.Speak(LocalizationService.Mark("No controller detected."));
            input.SetDeviceMode(_settings.DeviceMode);
            _raceInput = new RaceInput(_settings);
            _setup = new RaceSetup();
            _raceModeFactory = new RaceModeFactory(audio, speech, _settings, _raceInput, _fileDialogs);
            _stateMachine = new StateMachine(this);
            _menu = new MenuManager(audio, speech, () => _settings.UsageHints);
            _dialogs = new DialogManager(_menu, message => speech.Speak(message));
            _choices = new ChoiceDialogManager(_menu, message => speech.Speak(message));
            var pick = new Pick();
            var fmt = new ResultFmt(pick);
            var resultDialogs = new ResultDialogs(pick, fmt);
            _resultShow = new ResultShow(dialog => _dialogs.Show(dialog), PlayRaceWinSound, resultDialogs);
            _menu.SetWrapNavigation(_settings.MenuWrapNavigation);
            _menu.SetMenuSoundPreset(_settings.MenuSoundPreset);
            _menu.SetMenuNavigatePanning(_settings.MenuNavigatePanning);
            _menu.SetMenuAutoFocus(_settings.MenuAutoFocus);
            _selection = new RaceSelection(_setup, _settings);
            _menuRegistry = new MenuRegistry(_menu, _settings, _setup, _raceInput, _selection, this, this, this, this, this, this);
            _inputMapping = new InputMappingHandler(input, _raceInput, _settings, speech, SaveSettings);
            _shortcutMapping = new ShortcutMappingHandler(input, _menu, _settings, speech, SaveSettings);
            _updateConfig = UpdateConfig.Default;
            _updateService = new UpdateService(_updateConfig);
            _multiplayerCoordinator = new MultiplayerCoordinator(
                _menu,
                _dialogs,
                audio,
                speech,
                _settings,
                new MultiplayerConnector(),
                BeginPromptTextInput,
                SaveSettings,
                EnterMenuState,
                SetSession,
                GetSession,
                ClearSession,
                ResetPendingMultiplayerState,
                SetMultiplayerLoadout);
            _multiplayerRaceRuntime = new MultiplayerRaceRuntime(this);
            _multiplayerDispatch = new MultiplayerDispatch(this);
            _menuRegistry.RegisterAll();
            _multiplayerCoordinator.ConfigureMenuCloseHandlers();
            ApplySavedShortcutBindings();
            _settings.AudioVolumes ??= new AudioVolumeSettings();
            _settings.SyncAudioCategoriesFromMusicVolume();
            ApplyAudioSettings();
            _needsCalibration = _settings.UsageHints && _settings.ScreenReaderRateMs <= 0f;
        }

        public void Initialize()
        {
            _logo = new LogoScreen((AudioManager)_audio);
            _logo.Start();
            _state = AppState.Logo;
        }
    }
}


