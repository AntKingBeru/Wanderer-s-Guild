// Root component of the in-world adventurer prefab.
// Initialised by AdventurerWorldManager on spawn. Capsule color reflects the adventurer's
// current rank (read from QuestConfig.GetRankConfig). Billboard shows name, level, and HP.
// MaterialPropertyBlock is used so changing color never creates a material instance.

using UnityEngine;

public class AdventurerWorldObject : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The capsule Renderer whose colour reflects rank. Assign the Capsule child's Renderer.")]
    [SerializeField] private Renderer capsuleRenderer;
    
    [Tooltip("The billboard UI script on the World Space Canvas child.")]
    [SerializeField] private AdventurerBillboard billboard;
    
    private static readonly int ColorPropertyId = Shader.PropertyToID("_BaseColor");
    
    private MaterialPropertyBlock _mpb;
    private AdventurerData _adventurer;
    private QuestConfig _questConfig;

    public void Initialize(AdventurerData adventurer, QuestConfig questConfig)
    {
        _adventurer = adventurer;
        _questConfig = questConfig;
        _mpb = new MaterialPropertyBlock();
        Refresh();
    }

    public void Refresh()
    {
        if (_adventurer == null)
            return;
        RefreshCapsuleColor();
        RefreshBillboard();
    }

    private void RefreshCapsuleColor()
    {
        if (!capsuleRenderer || !_questConfig)
            return;
        var rankColor = _questConfig.GetRankConfig(_adventurer.Rank).CardColor;
        capsuleRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(ColorPropertyId, rankColor);
        capsuleRenderer.SetPropertyBlock(_mpb);
    }

    private void RefreshBillboard()
    {
        if (!billboard)
            return;
        var hpFraction = _adventurer.MaxHp > 0f
            ? _adventurer.CurrentHp / _adventurer.MaxHp
            : 1f;
        billboard.Refresh(_adventurer.Name, _adventurer.Level, hpFraction);
    }
    
    public string AdventurerId => _adventurer.Id;
}