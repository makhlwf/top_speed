using System;
using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Core;
using TopSpeed.Input;

using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private const string PreviousChatCategoryShortcutActionId = "chat_prev_category";
        private const string NextChatCategoryShortcutActionId = "chat_next_category";

        private readonly MenuManager _menu;
        private readonly RaceSettings _settings;
        private readonly RaceSetup _setup;
        private readonly RaceInput _raceInput;
        private readonly RaceSelection _selection;
        private readonly IMenuUiActions _ui;
        private readonly IMenuRaceActions _race;
        private readonly IMenuServerActions _server;
        private readonly IMenuSettingsActions _settingsActions;
        private readonly IMenuAudioActions _audio;
        private readonly IMenuMappingActions _mapping;
        private readonly IReadOnlyList<string> _menuSoundPresets;
        private readonly MenuView _sharedLobbyChatScreen;

        public MenuRegistry(
            MenuManager menu,
            RaceSettings settings,
            RaceSetup setup,
            RaceInput raceInput,
            RaceSelection selection,
            IMenuUiActions ui,
            IMenuRaceActions race,
            IMenuServerActions server,
            IMenuSettingsActions settingsActions,
            IMenuAudioActions audio,
            IMenuMappingActions mapping)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            _raceInput = raceInput ?? throw new ArgumentNullException(nameof(raceInput));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _race = race ?? throw new ArgumentNullException(nameof(race));
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _settingsActions = settingsActions ?? throw new ArgumentNullException(nameof(settingsActions));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _menuSoundPresets = LoadMenuSoundPresets();
            _sharedLobbyChatScreen = new MenuView(
                "shared_lobby_chat",
                new[] { new MenuItem(LocalizationService.Mark("No messages yet."), MenuAction.None) },
                title: LocalizationService.Mark("History"),
                preserveSelection: true,
                titleSpeakFlag: TopSpeed.Speech.SpeechService.SpeakFlag.None);
        }

        public void RegisterAll()
        {
            RegisterSharedLobbyChatShortcuts();
            RegisterMainMenu();

            _menu.Register(BuildMultiplayerMenu());
            _menu.Register(BuildMultiplayerServersMenu());
            _menu.Register(BuildMultiplayerSavedServersMenu());
            _menu.Register(BuildMultiplayerSavedServerFormMenu());
            _menu.Register(BuildMultiplayerLobbyMenu());
            _menu.Register(BuildMultiplayerRoomsMenu());
            _menu.Register(BuildMultiplayerCreateRoomMenu());
            _menu.Register(BuildMultiplayerRoomControlsMenu());
            _menu.Register(BuildMultiplayerRoomPlayersMenu());
            _menu.Register(BuildMultiplayerOnlinePlayersMenu());
            _menu.Register(BuildMultiplayerRoomOptionsMenu());
            _menu.Register(BuildMultiplayerRoomGameRulesMenu());
            _menu.Register(BuildMultiplayerRoomTrackTypeMenu());
            _menu.Register(BuildMultiplayerRoomTrackRaceMenu());
            _menu.Register(BuildMultiplayerRoomTrackAdventureMenu());
            _menu.Register(BuildMultiplayerLoadoutVehicleMenu());
            _menu.Register(BuildMultiplayerLoadoutTransmissionMenu());

            _menu.Register(BuildTrackTypeMenu("time_trial_type", RaceMode.TimeTrial));
            _menu.Register(BuildTrackTypeMenu("single_race_type", RaceMode.SingleRace));

            _menu.Register(BuildTrackMenu("time_trial_tracks_race", RaceMode.TimeTrial, TrackCategory.RaceTrack));
            _menu.Register(BuildTrackMenu("time_trial_tracks_adventure", RaceMode.TimeTrial, TrackCategory.StreetAdventure));
            _menu.Register(BuildCustomTrackMenu("time_trial_tracks_custom", RaceMode.TimeTrial));
            _menu.Register(BuildTrackMenu("single_race_tracks_race", RaceMode.SingleRace, TrackCategory.RaceTrack));
            _menu.Register(BuildTrackMenu("single_race_tracks_adventure", RaceMode.SingleRace, TrackCategory.StreetAdventure));
            _menu.Register(BuildCustomTrackMenu("single_race_tracks_custom", RaceMode.SingleRace));

            _menu.Register(BuildVehicleMenu("time_trial_vehicles", RaceMode.TimeTrial));
            _menu.Register(BuildCustomVehicleMenu("time_trial_vehicles_custom", RaceMode.TimeTrial));
            _menu.Register(BuildVehicleMenu("single_race_vehicles", RaceMode.SingleRace));
            _menu.Register(BuildCustomVehicleMenu("single_race_vehicles_custom", RaceMode.SingleRace));

            _menu.Register(BuildTransmissionMenu("time_trial_transmission", RaceMode.TimeTrial));
            _menu.Register(BuildTransmissionMenu("single_race_transmission", RaceMode.SingleRace));

            _menu.Register(BuildOptionsMenu());
            _menu.Register(BuildOptionsGameSettingsMenu());
            _menu.Register(BuildOptionsAudioSettingsMenu());
            _menu.Register(BuildOptionsVolumeSettingsMenu());
            _menu.Register(BuildOptionsControlsMenu());
            _menu.Register(BuildOptionsControlsDeviceMenu());
            _menu.Register(BuildOptionsControlsKeyboardMenu());
            _menu.Register(BuildOptionsControlsControllerMenu());
            _menu.Register(BuildOptionsControlsShortcutGroupsMenu());
            _menu.Register(BuildOptionsControlsShortcutBindingsMenu());
            _menu.Register(BuildOptionsRaceSettingsMenu());
            _menu.Register(BuildOptionsRestoreMenu());
            _menu.Register(BuildOptionsServerSettingsMenu());
        }

        private void RegisterSharedLobbyChatShortcuts()
        {
            _menu.RegisterShortcutAction(
                PreviousChatCategoryShortcutActionId,
                LocalizationService.Mark("Previous chat category"),
                LocalizationService.Mark("Switches chat history to the previous category in the shared lobby chat view."),
                Key.Left,
                _server.PreviousChatCategory);
            _menu.RegisterShortcutAction(
                NextChatCategoryShortcutActionId,
                LocalizationService.Mark("Next chat category"),
                LocalizationService.Mark("Switches chat history to the next category in the shared lobby chat view."),
                Key.Right,
                _server.NextChatCategory);
            _menu.SetViewShortcutActions(
                _sharedLobbyChatScreen.Id,
                new[] { PreviousChatCategoryShortcutActionId, NextChatCategoryShortcutActionId },
                LocalizationService.Mark("Shared lobby chat"));
        }

        private void RegisterMainMenu()
        {
            var mainMenu = _menu.CreateMenu("main", new[]
            {
                new MenuItem(LocalizationService.Mark("Quick start"), MenuAction.QuickStart),
                new MenuItem(LocalizationService.Mark("Time trial"), MenuAction.None, nextMenuId: "time_trial_type", onActivate: () => PrepareMode(RaceMode.TimeTrial)),
                new MenuItem(LocalizationService.Mark("Single race"), MenuAction.None, nextMenuId: "single_race_type", onActivate: () => PrepareMode(RaceMode.SingleRace)),
                new MenuItem(LocalizationService.Mark("MultiPlayer game"), MenuAction.None, nextMenuId: "multiplayer"),
                new MenuItem(LocalizationService.Mark("Options"), MenuAction.None, nextMenuId: "options_main"),
                new MenuItem(LocalizationService.Mark("Check for updates"), MenuAction.None, onActivate: _settingsActions.CheckForUpdates),
                new MenuItem(LocalizationService.Mark("Exit Game"), MenuAction.Exit)
            }, LocalizationService.Mark("Main menu"), titleProvider: MainMenuTitle);

            mainMenu.MusicFile = "theme1.ogg";
            mainMenu.MusicVolume = _settings.MusicVolume;
            mainMenu.MusicVolumeChanged = _audio.SaveMusicVolume;
            _menu.Register(mainMenu);
        }
    }
}





