using UnityEngine;

namespace QuestSystem.UI
{
    /// <summary>
    /// Attach to the in-world Quest Creator 3-D object.
    /// Drag the QuestCreatorUI panel into the inspector.
    /// </summary>
    public class QuestCreatorInteractable : WorldInteractable
    {
        [Header("Panel Reference")]
        [SerializeField] private QuestCreatorUI creatorUI;

        protected override void OpenPanel()
        {
            if (!creatorUI)
            {
                Debug.LogWarning("[QuestCreatorInteractable] No QuestCreatorUI assigned.");
                return;
            }

            creatorUI.Show();
            SetOpen(true);
        }

        protected override void ClosePanel()
        {
            if (!creatorUI)
                return;

            creatorUI.Hide();
            SetOpen(false);
        }
    }
}