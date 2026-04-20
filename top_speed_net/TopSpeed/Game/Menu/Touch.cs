using TS.Sdl.Input;
using TopSpeed.Input;
using TopSpeed.Menu;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private bool _multiplayerMenuTouchZonesApplied;

        private void UpdateMultiplayerMenuTouchControls()
        {
            if (!ShouldApplyMultiplayerMenuTouchLayout())
            {
                if (_multiplayerMenuTouchZonesApplied)
                {
                    // Avoid clearing race touch zones when we have already switched to race state.
                    if (!_driveTouchZonesApplied)
                        _input.ClearTouchZones();
                    _multiplayerMenuTouchZonesApplied = false;
                }

                return;
            }

            EnsureMultiplayerMenuTouchZones();
            HandleMultiplayerTopZoneGestures();
        }

        private bool ShouldApplyMultiplayerMenuTouchLayout()
        {
            if (!_isAndroidPlatform || _state != AppState.Menu)
                return false;

            return MenuTouchProfile.UsesMultiplayerZones(_menu.CurrentId);
        }

        private void EnsureMultiplayerMenuTouchZones()
        {
            if (_multiplayerMenuTouchZonesApplied)
                return;

            _input.SetTouchZones(new[]
            {
                new TouchZone(
                    MenuTouchProfile.MultiplayerTopZoneId,
                    new TouchZoneRect(0f, 0f, 1f, MenuTouchProfile.MultiplayerSplitY),
                    priority: 20,
                    behavior: TouchZoneBehavior.Lock),
                new TouchZone(
                    MenuTouchProfile.MultiplayerBottomZoneId,
                    new TouchZoneRect(0f, MenuTouchProfile.MultiplayerSplitY, 1f, 1f - MenuTouchProfile.MultiplayerSplitY),
                    priority: 20,
                    behavior: TouchZoneBehavior.Lock)
            });
            _multiplayerMenuTouchZonesApplied = true;
        }

        private void HandleMultiplayerTopZoneGestures()
        {
            if (_textInputPromptActive
                || _dialogs.HasActiveOverlayDialog
                || _choices.HasActiveChoiceDialog
                || _multiplayerCoordinator.Questions.HasActiveOverlayQuestion)
            {
                return;
            }

            if (_input.WasZoneGesturePressed(GestureIntent.SwipeUp, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerCoordinator.NextChatCategory();
            else if (_input.WasZoneGesturePressed(GestureIntent.SwipeDown, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerCoordinator.PreviousChatCategory();

            if (_input.WasZoneGesturePressed(GestureIntent.SwipeRight, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerCoordinator.NextChatItem();
            else if (_input.WasZoneGesturePressed(GestureIntent.SwipeLeft, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerCoordinator.PreviousChatItem();

            if (_input.WasZoneGesturePressed(GestureIntent.DoubleTap, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerCoordinator.CheckPing();

            if (_input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeRight, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerCoordinator.OpenGlobalChatHotkey();
            else if (_input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeLeft, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerCoordinator.OpenRoomChatHotkey();
        }
    }
}
