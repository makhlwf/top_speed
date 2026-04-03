using System;
using System.Globalization;
using TopSpeed.Localization;
using TopSpeed.Race;

namespace TopSpeed.Game
{
    internal sealed class ResultFmt
    {
        private readonly Pick _pick;

        public ResultFmt(Pick pick)
        {
            _pick = pick ?? throw new ArgumentNullException(nameof(pick));
        }

        public string Line(RaceResultEntry entry)
        {
            var template = PickLineTemplate(entry.Position);
            var playerName = string.IsNullOrWhiteSpace(entry.Name)
                ? LocalizationService.Mark("Player")
                : entry.Name.Trim();
            return LocalizationService.Format(
                template,
                playerName,
                entry.Position,
                Time(entry.TimeMs));
        }

        public string Time(int timeMs)
        {
            var clamped = Math.Max(0, timeMs);
            var minutes = clamped / 60000;
            var seconds = (clamped % 60000) / 1000;
            var minuteText = LocalizationService.Format(
                minutes == 1
                    ? LocalizationService.Mark("{0} minute")
                    : LocalizationService.Mark("{0} minutes"),
                minutes.ToString(CultureInfo.InvariantCulture));
            var secondText = LocalizationService.Format(
                seconds == 1
                    ? LocalizationService.Mark("{0} second")
                    : LocalizationService.Mark("{0} seconds"),
                seconds.ToString(CultureInfo.InvariantCulture));

            if (minutes > 0)
            {
                return LocalizationService.Format(
                    LocalizationService.Mark("{0} and {1}"),
                    minuteText,
                    secondText);
            }

            return secondText;
        }

        private string PickLineTemplate(int position)
        {
            if (position <= 1)
                return _pick.One(ResultCatalog.FirstPlaceLineTemplates);
            if (position <= 3)
                return _pick.One(ResultCatalog.PodiumLineTemplates);
            return _pick.One(ResultCatalog.FieldLineTemplates);
        }
    }
}

