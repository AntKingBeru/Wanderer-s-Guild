// The quest board in the world: one paper mesh per board slot, drawn only while that slot holds a quest (the same slots as
// the Quest Board screen).
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: On the Guild Hall prefab's quest board. Observer — refreshes on OnQuestBoardChanged and when enabled (the hall can
    // spawn before or after the quests load). Hides by Renderer.enabled, NOT SetActive: the board's GuildInteractable reads
    // every child renderer's bounds for its label and fitted collider, and an inactive object's bounds are empty at the origin.
    public class QuestBoardPapers : MonoBehaviour
    {
        [Tooltip("One paper per board slot, in slot order — slot 1 first, matching the Quest Board screen (5 per row, top row " +
                 "first). Extra papers beyond the slot count stay hidden.")]
        [SerializeField] private Renderer[] papers = System.Array.Empty<Renderer>();

        private void OnEnable()
        {
            GameEventsRelay.OnQuestBoardChanged.AddListener(Refresh);
            Refresh();
        }

        private void OnDisable() => GameEventsRelay.OnQuestBoardChanged.RemoveListener(Refresh);

        private void Refresh()
        {
            var slots = QuestManager.HasInstance ? QuestManager.Instance.BoardSlots : null;
            for (var i = 0; i < papers.Length; i++)
            {
                if (papers[i] != null)
                    papers[i].enabled = slots != null && i < slots.Count && slots[i] != null;
            }
        }
    }
}