using UnityEngine;
using TMPro;

namespace QuestSystem.UI
{
    /// <summary>
    /// Shows a numeric badge on a UI element.
    /// Increment when a new request arrives; decrement when a quest is created.
    /// Attach near the world-space button UI or any HUD icon.
    /// </summary>
    public class QuestNotificationBadge : MonoBehaviour
    {
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private TextMeshProUGUI countText;

        private int _count;

        private void Start()
        {
            var questManager = QuestManager.Instance;
            if (!questManager)
                return;

            questManager.OnRequestReceived += _ => SetCount(_count + 1);
            questManager.OnQuestCreated += _ => SetCount(Mathf.Max(0, _count - 1));

            SetCount(0);
        }

        private void SetCount(int value)
        {
            _count = value;

            if (countText)
                countText.text = _count.ToString();

            if (badgeRoot)
                badgeRoot.SetActive(_count > 0);
        }
    }
}