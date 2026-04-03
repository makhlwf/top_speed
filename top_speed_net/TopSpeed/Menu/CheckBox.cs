using System;
using TopSpeed.Localization;

namespace TopSpeed.Menu
{
    internal sealed class CheckBox : MenuItem
    {
        private readonly Func<bool> _getValue;
        private readonly Action<bool> _setValue;
        private readonly Action<bool>? _onChanged;

        public CheckBox(
            string text,
            Func<bool> getValue,
            Action<bool> setValue,
            Action<bool>? onChanged = null,
            MenuAction action = MenuAction.None,
            string? nextMenuId = null,
            Action? onActivate = null,
            bool suppressPostActivateAnnouncement = false,
            string? hint = null)
            : base(text, action, nextMenuId, onActivate, suppressPostActivateAnnouncement, hint)
        {
            _getValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
            _setValue = setValue ?? throw new ArgumentNullException(nameof(setValue));
            _onChanged = onChanged;
        }

        public override string GetDisplayText()
        {
            var typeLabel = LocalizationService.Translate(LocalizationService.Mark("check box"));
            return $"{GetBaseText()} {typeLabel} {FormatValue(_getValue())}";
        }

        public override string? ActivateAndGetAnnouncement()
        {
            var newValue = !_getValue();
            _setValue(newValue);
            _onChanged?.Invoke(newValue);
            base.ActivateAndGetAnnouncement();
            return FormatValue(newValue);
        }

        private static string FormatValue(bool value)
        {
            return value
                ? LocalizationService.Translate(LocalizationService.Mark("checked"))
                : LocalizationService.Translate(LocalizationService.Mark("not checked"));
        }
    }
}

