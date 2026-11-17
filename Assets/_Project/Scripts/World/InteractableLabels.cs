// The hover text of a guild object: its name, plus a live count where one helps — the quest board's "(posted/open)".
namespace WanderersGuild
{
    // NOTE: Strategy by the screen the object opens; add a case here for any other object that should show a count.
    public static class InteractableLabels
    {
        public static string HoverText(string name, MenuPanelId opens) => opens switch
        {
            MenuPanelId.Quests when QuestManager.HasInstance =>
                $"{name} ({QuestManager.Instance.Quests.Count}/{QuestManager.Instance.OpenSlots})",
            _ => name
        };
    }
}