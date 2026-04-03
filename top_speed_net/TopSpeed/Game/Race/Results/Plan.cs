using System;
using TopSpeed.Menu;

namespace TopSpeed.Game
{
    internal sealed class ResultPlan
    {
        public ResultPlan(Dialog dialog, bool playWin)
        {
            Dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            PlayWin = playWin;
        }

        public Dialog Dialog { get; }
        public bool PlayWin { get; }
    }
}

