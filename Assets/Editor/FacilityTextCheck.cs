// Text-length check for facilities: catalog card name, info and limit lines, and the room card title at the highest level.
using System;
using WanderersGuild;
using Object = UnityEngine.Object;

namespace Editor
{
    public sealed class FacilityTextCheck : ITextLengthCheck
    {
        // NOTE: Uses the same formatters as the cards, so a format change is checked too.
        public int Run(Action<string, Object> report)
        {
            var count = 0;
            foreach (var data in TextLengthValidator.LoadAll<FacilityData>())
            {
                TextLengthValidator.Check(TextLimits.BuildCardLine, data.DisplayName, data.Id, data, report);
                TextLengthValidator.Check(TextLimits.BuildCardLine, FacilityFormatter.CardInfo(data), data.Id, data, report);
                TextLengthValidator.Check(TextLimits.RoomCardTitle, FacilityFormatter.TitleFor(data, data.MaxLevel), data.Id, data, report);
                count += 3;

                if (data.Limit <= 0)
                    continue;
                TextLengthValidator.Check(TextLimits.BuildCardLine, FacilityRules.LimitReason(data.Limit), data.Id, data, report);
                count++;
            }
            return count;
        }
    }
}