using System;
using System.Text;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static string PrettyPrintJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "{}";

            var sb = new StringBuilder(json.Length + 256);
            var indentLevel = 0;
            var inString = false;
            var escaping = false;

            for (var i = 0; i < json.Length; i++)
            {
                var c = json[i];
                if (inString)
                {
                    sb.Append(c);
                    if (escaping)
                    {
                        escaping = false;
                    }
                    else if (c == '\\')
                    {
                        escaping = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (char.IsWhiteSpace(c))
                    continue;

                switch (c)
                {
                    case '"':
                        inString = true;
                        sb.Append(c);
                        break;
                    case '{':
                    case '[':
                    {
                        var closing = c == '{' ? '}' : ']';
                        var nextIndex = NextNonWhitespaceIndex(json, i + 1);
                        if (nextIndex >= 0 && json[nextIndex] == closing)
                        {
                            sb.Append(c);
                            sb.Append(closing);
                            i = nextIndex;
                            break;
                        }

                        sb.Append(c);
                        sb.AppendLine();
                        indentLevel++;
                        AppendIndent(sb, indentLevel);
                        break;
                    }
                    case '}':
                    case ']':
                        sb.AppendLine();
                        indentLevel = Math.Max(0, indentLevel - 1);
                        AppendIndent(sb, indentLevel);
                        sb.Append(c);
                        break;
                    case ',':
                        sb.Append(c);
                        sb.AppendLine();
                        AppendIndent(sb, indentLevel);
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        private static int NextNonWhitespaceIndex(string text, int start)
        {
            for (var i = start; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                    return i;
            }

            return -1;
        }

        private static void AppendIndent(StringBuilder sb, int indentLevel)
        {
            for (var i = 0; i < indentLevel; i++)
            {
                sb.Append("  ");
            }
        }
    }
}

