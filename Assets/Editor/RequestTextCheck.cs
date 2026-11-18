// Text-length check for requests: the title (one line) and the description (6 lines) — same width on the board and the note.
using System;
using WanderersGuild;
using Object = UnityEngine.Object;

namespace Editor
{
    public sealed class RequestTextCheck : ITextLengthCheck
    {
        public int Run(Action<string, Object> report)
        {
            var count = 0;
            foreach (var data in TextLengthValidator.LoadAll<RequestData>())
            {
                TextLengthValidator.Check(TextLimits.RequestTitle, data.DisplayName, data.Id, data, report);
                TextLengthValidator.Check(TextLimits.RequestObjective, data.Objective, data.Id, data, report);
                count += 2;
            }
            return count;
        }
    }
}