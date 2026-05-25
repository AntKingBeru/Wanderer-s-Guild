using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuestSystem.UI
{
    /// <summary>
    /// One row in the "available requests" list on the left panel of the Quest Creator UI.
    /// Clicking it fires OnSelected, which the parent UI uses to open the request pop-up.
    /// </summary>
    public class RequestListItemUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private Button selectButton;
 
        public QuestRequest Request { get; private set; }
        public event Action<QuestRequest> OnSelected;
 
        private void Awake()
        {
            selectButton.onClick.AddListener(() => OnSelected?.Invoke(Request));
        }
 
        public void Bind(QuestRequest request)
        {
            Request = request;
            nameText.text     = request.requestName;
            categoryText.text = request.category.ToString();
        }
    }
}