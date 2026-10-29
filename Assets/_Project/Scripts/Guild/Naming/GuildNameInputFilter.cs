using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class GuildNameInputFilter : IDisposable
    {
        private readonly TextField _field;
        private readonly GuildNameRules _rules;
        private readonly Action<string> _onChanged;

        public GuildNameInputFilter(TextField field, GuildNameRules rules, Action<string> onChanged)
        {
            _field = field;
            _rules = rules;
            _onChanged = onChanged;

            _field.maxLength = rules.MaxLength;
            _field.RegisterValueChangedCallback(OnValueChanged);
        }

        public void Dispose() => _field.UnregisterValueChangedCallback(OnValueChanged);

        // NOTE: The caret moves back by however many characters were removed, so it stays where the player was typing.
        private void OnValueChanged(ChangeEvent<string> evt)
        {
            var raw = evt.newValue ?? string.Empty;
            var filtered = _rules.Filter(raw);

            if (filtered != raw)
            {
                var caret = Mathf.Clamp(_field.cursorIndex - (raw.Length - filtered.Length), 0, filtered.Length);
                _field.SetValueWithoutNotify(filtered);
                _field.SelectRange(caret, caret);
            }

            _onChanged?.Invoke(filtered);
        }
    }
}