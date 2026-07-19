// Builds the billboard's label ("{level}|{name}|{class}{rank}") and class image into a world UIDocument.

using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class BillboardContent : MonoBehaviour
{
    [SerializeField] private RankPalette palette;

    private VisualElement _classImage;
    private VisualElement _rankSpan;
    private Label _label;

    private void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _classImage = root.Q<VisualElement>("class-image");
        _rankSpan = root.Q<VisualElement>("rank-span");
        _label = root.Q<Label>("info-label");
    }

    public void Render(BillboardInfo info, Sprite classSprite)
    {
        if (_classImage != null && classSprite)
            _classImage.style.backgroundImage = new StyleBackground(classSprite);
        
        if (_label != null)
            _label.text = $"{info.level}|{info.name}|{info.@class}";

        var rankLabel = _rankSpan?.Q<Label>("rank-label");
        if (rankLabel != null)
        {
            rankLabel.text = info.rank.ToString();
            rankLabel.style.color = palette ? palette.GetColor(info.rank) : Color.white;
        }
    }
}