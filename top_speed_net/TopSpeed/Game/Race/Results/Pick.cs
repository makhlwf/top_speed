using System;
using TopSpeed.Common;

namespace TopSpeed.Game
{
    internal sealed class Pick
    {
        private readonly Func<int, int> _next;

        public Pick(Func<int, int>? next = null)
        {
            _next = next ?? Algorithm.RandomInt;
        }

        public string One(string[] options)
        {
            if (options == null || options.Length == 0)
                return string.Empty;
            if (options.Length == 1)
                return options[0];
            return options[_next(options.Length)];
        }
    }
}

