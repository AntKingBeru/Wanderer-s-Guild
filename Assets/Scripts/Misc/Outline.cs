// Toggles an inverted-hull outline by appending an outline material to this object's renderer(s).

using System.Collections.Generic;
using UnityEngine;

public class Outline : MonoBehaviour
{
    private static readonly int OutlineColor = Shader.PropertyToID("outline_color");
    private static readonly int OutlineWidth = Shader.PropertyToID("outline_width");
    
    [SerializeField] private Color color = new(0.40f, 0.80f, 1.0f, 1.0f);
    [Tooltip("Extrusion in world units. ~0.06–0.12 reads as a clear glow border on a unit-scale prop.")]
    [SerializeField] private float width = 0.08f;
    
    [Tooltip("Include child renderers. Leave OFF if children are separate objects that shouldn't glow.")]
    [SerializeField] private bool includeChildren;
    
    private Renderer[] _renderers;
    private Material _outlineMaterial;
    private Material[][] _originalMaterials;
    private bool _visible;
    
    private void Awake()
    {
        _renderers = includeChildren
            ? GetComponentsInChildren<Renderer>()
            : GetComponents<Renderer>();
        
        if (_renderers.Length == 0)
        {
            Debug.LogWarning($"[Outline] No Renderer found on '{name}'.", this);
            enabled = false;
            return;
        }
        
        _originalMaterials = new Material[_renderers.Length][];
        for (var i = 0; i < _renderers.Length; i++)
            _originalMaterials[i] = _renderers[i].sharedMaterials;

        var shader = Shader.Find("Wanderer/OutlineExtrude");
        if (!shader)
        {
            Debug.LogError("[Outline] Shader 'Wanderer/OutlineExtrude' not found.");
            enabled = false;
            return;
        }

        _outlineMaterial = new Material(shader);
        _outlineMaterial.SetColor(OutlineColor, color);
        _outlineMaterial.SetFloat(OutlineWidth, width);
    }
    
    public void Show()
    {
        if (_visible || !_outlineMaterial)
            return;
        _visible = true;

        for (var i = 0; i < _renderers.Length; i++)
        {
            var src = _originalMaterials[i];
            var combined = new Material[src.Length + 1];
            System.Array.Copy(src, combined, src.Length);
            combined[src.Length] = _outlineMaterial;
            _renderers[i].sharedMaterials = combined;
        }
    }
    
    public void Hide()
    {
        if (!_visible)
            return;
        _visible = false;
        for (var i = 0; i < _renderers.Length; i++)
            _renderers[i].sharedMaterials = _originalMaterials[i];
    }
    
    public void SetColor(Color c)
    {
        color = c;
        _outlineMaterial?.SetColor(OutlineColor, c);
    }
    
    public void SetWidth(float w)
    {
        width = w;
        _outlineMaterial?.SetFloat(OutlineWidth, w);
    }

    private void OnDestroy()
    {
        if (_outlineMaterial)
            Destroy(_outlineMaterial);
    }
}