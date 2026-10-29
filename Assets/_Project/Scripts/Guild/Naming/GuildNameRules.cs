using System.Linq;
using System.Text;

namespace WanderersGuild
{
    public sealed class GuildNameRules
    {
        private readonly NewGuildSettings _settings;

        public GuildNameRules(NewGuildSettings settings) => _settings = settings;

        public int MaxLength => _settings.NameMaxLength;

        // NOTE: ASCII letters only for now — the pixel font's coverage beyond ASCII isn't verified.
        public static bool IsAllowed(char c) =>
            c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or ' ' or '\'' or '-';

        // What the field may contain while typing: allowed characters only, no leading or double spaces, capped.
        public string Filter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            StringBuilder builder = new(input.Length);
            foreach (var c in input.TakeWhile(c => builder.Length < MaxLength).Where(IsAllowed).Where(c => c != ' ' || (builder.Length != 0 && builder[^1] != ' ')))
            {
                builder.Append(c);
            }
            return builder.ToString();
        }

        // The name as it will be saved: filtered, trailing space removed.
        public string Finalize(string input) => Filter(input).TrimEnd();

        public bool IsValid(string input)
        {
            var name = Finalize(input);
            if (name.Length < _settings.NameMinLength || name.Length > MaxLength)
                return false;
            
            return name.Any(char.IsLetterOrDigit);
        }
    }
}