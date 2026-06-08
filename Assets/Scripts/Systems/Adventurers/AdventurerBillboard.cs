// Attached to tje World Space Canvas child of an AdventurerWorldObject prefab.
// Faces the main camera every LateUpdate.
// Displays the adventurer's name with level in parentheses, and a color-coded HP bar

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdventurerBillboard : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TMP_Text nameLabel;
    
    [Header("HP Bar")]
    [SerializeField] private Image hpBar;

    [Header("HP Color Thresholds")]
    [SerializeField] private Color highHp = new (0.18f, 0.8f, 0.44f);
    [SerializeField] private Color midHp = new (0.95f, 0.77f, 0.06f);
    [SerializeField] private Color lowHp = new (0.91f, 0.3f, 0.24f);
    [SerializeField, Range(0f, 1f)] private float midThreshold = 0.5f;
    [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.25f;

    private Camera _cam;
    
    private void Awake() => _cam = Camera.main;

    private void LateUpdate()
    {
        if (!_cam)
        {
            _cam = Camera.main;
            return;
        }
        transform.LookAt(transform.position + _cam.transform.rotation * Vector3.forward, _cam.transform.rotation * Vector3.up);
    }

    public void Refresh(string adventurerName, int level, float hpFraction)
    {
        hpFraction = Mathf.Clamp01(hpFraction);
        if (nameLabel)
            nameLabel.text = $"{adventurerName} <size=75%>({level})</size>";
        if (hpBar)
        {
            hpBar.fillAmount = hpFraction;
            hpBar.color = hpFraction > midThreshold ? highHp
                : hpFraction > lowThreshold ? midHp
                : lowHp;
        }
    }
}