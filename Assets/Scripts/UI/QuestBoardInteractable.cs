using UnityEngine;

namespace QuestSystem.UI
{
    /// <summary>
    /// Attach to the in-world Quest Board 3-D object.
    /// Drag the QuestBoardUI panel into the inspector.
    /// </summary>
    public class QuestBoardInteractable : WorldInteractable
    {
        [Header("Panel Reference")]
        [SerializeField] private QuestBoardUI boardUI;

        protected override void OpenPanel()
        {
            if (!boardUI)
            {
                Debug.LogWarning("[QuestBoardInteractable] No QuestBoardUI assigned.");
                return;
            }

            boardUI.Show(this);
            SetOpen(true);
        }

        protected override void ClosePanel()
        {
            if (!boardUI)
                return;

            boardUI.Hide();
        }
    }
}