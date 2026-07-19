// Gold HUD view: holds the amount label and floater container; formats the displayed total.

using UnityEngine.UIElements;

public class GoldHudView
{
    private readonly Label _amount;
    private readonly VisualElement _floaterLayer;
    
    public VisualElement FloaterLayer => _floaterLayer;

    public GoldHudView(VisualElement root)
    {
        _amount = root.Q<Label>("gold-amount");
        _floaterLayer = root.Q<VisualElement>("gold-floaters");
    }
    
    public void SetAmount(int value)
    {
        if (_amount != null)
            _amount.text = value.ToString("N0");
    }
}