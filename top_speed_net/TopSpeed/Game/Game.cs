using System;
using TopSpeed.Menu;

namespace TopSpeed.Game
{
    internal sealed partial class Game : IDisposable,
        IMenuUiActions,
        IMenuRaceActions,
        IMenuServerActions,
        IMenuSettingsActions,
        IMenuAudioActions,
        IMenuMappingActions
    {
    }
}

