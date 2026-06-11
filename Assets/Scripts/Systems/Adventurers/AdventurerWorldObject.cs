// Root component of the in-world adventurer prefab.
// Replaces the capsule renderer with a SpriteRenderer that always faces the camera.
// The sprite is swapped based on the adventurer's class; rank tint is applied via
// MaterialPropertyBlock so no material instances are created.
// The billboard overlay (name + HP bar) remains on the World Space Canvas child.

using UnityEngine;

public class AdventurerWorldObject : MonoBehaviour
{
    [Header("Sprite Billboard")]
    [Tooltip("The SpriteRenderer on the child that displays the adventurer sprite. " +
             "This object faces the camera automatically each LateUpdate.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("One sprite per AdventurerClass, indexed by AdventurerClass enum int value. " +
             "Length must equal the number of AdventurerClass entries.")]
    [SerializeField] private Sprite[] classSprites;

    [Header("UI Billboard")]
    [Tooltip("The AdventurerBillboard on the World Space Canvas child.")]
    [SerializeField] private AdventurerBillboard billboard;
    
    [Header("Navigation")]
    [Tooltip("The navigation controller driving this adventurer's movement. " +
             "Assign the NavController component on this prefab.")]
    [SerializeField] private AdventurerNavigationController navController;

    // Cached property ID avoids string lookups each frame.
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock _mpb;
    private AdventurerData _adventurer;
    private QuestConfig _questConfig;
    private Camera _mainCam;

    // Called by AdventurerWorldManager immediately after instantiation.
    public void Initialize(AdventurerData adventurer, QuestConfig questConfig, Transform patrolCenter)
    {
        _adventurer = adventurer;
        _questConfig = questConfig;
        _mpb = new MaterialPropertyBlock();
        _mainCam  = Camera.main;
        Refresh();
        navController?.InitializeNavigation(adventurer, patrolCenter);
    }

    private void LateUpdate()
    {
        if (!_mainCam)
        {
            _mainCam = Camera.main;
            return;
        }
        
        if (!spriteRenderer)
            return;

        // Rotate the sprite to face the camera while keeping the sprite upright.
        var camRot = _mainCam.transform.rotation;
        spriteRenderer.transform.rotation = Quaternion.LookRotation(
            camRot * Vector3.forward,
            camRot * Vector3.up
        );
    }
    
    public void Refresh()
    {
        if (_adventurer == null)
            return;
        RefreshSprite();
        RefreshBillboard();
    }
    
    // Called by SoloAdventurerManager (via AdventurerWorldManager) when an application is submitted,
    // so the adventurer visually walks to the board.
    public void NotifyApplicationSubmitted()
        => navController?.TriggerBrowse();
    
    // Selects the sprite matching the adventurer's class and tints it with the rank color.
    private void RefreshSprite()
    {
        if (!spriteRenderer) return;

        // Swap sprite by class index.
        var classIndex = (int)_adventurer.Class;
        if (classSprites != null && classIndex < classSprites.Length && classSprites[classIndex])
            spriteRenderer.sprite = classSprites[classIndex];

        // Tint by rank color using a MaterialPropertyBlock to avoid material instancing.
        if (_questConfig)
        {
            var rankColor = _questConfig.GetRankConfig(_adventurer.Rank).CardColor;
            spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorPropertyId, rankColor);
            spriteRenderer.SetPropertyBlock(_mpb);
        }
    }

    // Pushes name, level, and HP fraction to the UI billboard.
    private void RefreshBillboard()
    {
        if (!billboard) return;
        var hpFraction = _adventurer.MaxHp > 0f
            ? _adventurer.CurrentHp / _adventurer.MaxHp
            : 1f;
        billboard.Refresh(_adventurer.Name, _adventurer.Level, hpFraction);
    }

    // Unique adventurer ID — used by AdventurerWorldManager to find this object.
    public string AdventurerId => _adventurer?.Id;
}