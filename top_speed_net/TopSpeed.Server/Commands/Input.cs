using System;
using System.Collections.Generic;
using System.Globalization;
using TopSpeed.Server.Logging;

namespace TopSpeed.Server.Commands
{
    internal static class CommandInput
    {
        public static bool TryReadLine(string? prompt, out string value)
        {
            value = string.Empty;
            if (!string.IsNullOrWhiteSpace(prompt))
                ConsoleSink.WriteLine(prompt);

            try
            {
                var line = Console.ReadLine();
                if (line == null)
                    return false;

                value = line;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
        }

        public static bool TryPromptMenuChoice(string title, IReadOnlyList<string> options, out int selectedIndex)
        {
            selectedIndex = -1;
            if (options == null || options.Count == 0)
                return false;

            ConsoleSink.WriteLine(title);
            for (var i = 0; i < options.Count; i++)
                ConsoleSink.WriteLine($"{i + 1}. {options[i]}");

            while (true)
            {
                if (!TryReadLine("Enter option number:", out var raw))
                    return false;

                if (!int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var menuNumber))
                {
                    ConsoleSink.WriteLine("Invalid input. Enter a valid number.");
                    continue;
                }

                var index = menuNumber - 1;
                if (index < 0 || index >= options.Count)
                {
                    ConsoleSink.WriteLine("Invalid option number.");
                    continue;
                }

                selectedIndex = index;
                return true;
            }
        }

        public static bool TryPromptInt(string prompt, int min, int max, out int value)
        {
            value = 0;
            while (true)
            {
                if (!TryReadLine(prompt, out var raw))
                    return false;

                if (!int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    ConsoleSink.WriteLine("Invalid input. Enter a valid number.");
                    continue;
                }

                if (parsed < min || parsed > max)
                {
                    ConsoleSink.WriteLine($"Value must be between {min} and {max}.");
                    continue;
                }

                value = parsed;
                return true;
            }
        }

        public static bool TryPromptText(string prompt, int maxLength, bool allowEmpty, out string value)
        {
            value = string.Empty;
            while (true)
            {
                if (!TryReadLine(prompt, out var raw))
                    return false;

                var text = raw.Trim();
                if (!allowEmpty && text.Length == 0)
                {
                    ConsoleSink.WriteLine("Value cannot be empty.");
                    continue;
                }

                if (text.Length > maxLength)
                {
                    ConsoleSink.WriteLine($"Value is too long. Maximum length is {maxLength} characters.");
                    continue;
                }

                value = text;
                return true;
            }
        }

        public static string FormatOnOff(bool value)
        {
            return value ? "on" : "off";
        }
    }
}
