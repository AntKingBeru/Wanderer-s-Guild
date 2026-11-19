// Mesh-only primitives without colliders: CreatePrimitive(Quad) also adds a MeshCollider, a class the web build strips.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Use this instead of GameObject.CreatePrimitive for any quad that never needs physics (sprites, footprints).
    public static class PrimitiveObjects
    {
        private static Mesh _quad;

        // Unity's own Quad mesh (1×1, centred, the same UVs CreatePrimitive uses) — shared, never modified.
        public static Mesh QuadMesh
        {
            get
            {
                if (_quad == null)
                    _quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                return _quad;
            }
        }

        // What CreatePrimitive(Quad) makes, minus the collider. NOTE: The renderer has no material — callers assign one.
        public static GameObject CreateQuad(string name)
        {
            GameObject quad = new(name);
            quad.AddComponent<MeshFilter>().sharedMesh = QuadMesh;
            quad.AddComponent<MeshRenderer>();
            return quad;
        }
    }
}