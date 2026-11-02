// Editor check: does each facility's level prefab sit on its footprint (mesh bounds from 0,0 to W×D in the prefab's space)?
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class FacilityPrefabValidator
    {
        private const float Tolerance = 0.25f; // Walls may stick out a little.

        // NOTE: Uses mesh bounds transformed into the prefab root's space (Renderer.bounds is empty on un-instantiated prefabs).
        [MenuItem("Tools/Wanderer's Guild/Facilities/Validate Prefabs")]
        private static void Validate()
        {
            var problems = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:FacilityData"))
            {
                var data = AssetDatabase.LoadAssetAtPath<FacilityData>(AssetDatabase.GUIDToAssetPath(guid));
                if (!data)
                    continue;

                for (var level = 1; level <= data.MaxLevel; level++)
                {
                    var prefab = data.PrefabFor(level);
                    if (!prefab)
                    {
                        Debug.LogWarning($"[Validate] {data.Id}: no prefab for level {level}.", data);
                        problems++;
                        continue;
                    }

                    if (!TryLocalBounds(prefab, out var min, out var max))
                    {
                        Debug.LogWarning($"[Validate] {data.Id} L{level}: '{prefab.name}' has no meshes.", prefab);
                        problems++;
                        continue;
                    }

                    Vector2 size = data.Size;
                    var cornerOff = Mathf.Abs(min.x) > Tolerance || Mathf.Abs(min.y) > Tolerance;
                    var sizeOff = Mathf.Abs(max.x - size.x) > Tolerance || Mathf.Abs(max.y - size.y) > Tolerance;
                    if (!cornerOff && !sizeOff)
                        continue;

                    problems++;
                    Debug.LogWarning($"[Validate] {data.Id} L{level} '{prefab.name}': model spans x {min.x:0.##}…{max.x:0.##}, " +
                                     $"z {min.y:0.##}…{max.y:0.##}; footprint is 0…{size.x} × 0…{size.y}" +
                                     (cornerOff ? " — pivot isn't at the min corner" : "") +
                                     (sizeOff ? " — size doesn't match the sheet's SIZE" : "") + ".", prefab);
                }
            }

            Debug.Log(problems == 0 ? "[Validate] Every facility prefab sits on its footprint." : $"[Validate] {problems} problem(s) found.");
        }

        private static bool TryLocalBounds(GameObject prefab, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);
            var any = false;
            var toRoot = prefab.transform.worldToLocalMatrix;

            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                any |= Encapsulate(filter.sharedMesh, toRoot * filter.transform.localToWorldMatrix, ref min, ref max);
            foreach (var skinned in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                any |= Encapsulate(skinned.sharedMesh, toRoot * skinned.transform.localToWorldMatrix, ref min, ref max);

            return any;
        }

        private static bool Encapsulate(Mesh mesh, Matrix4x4 matrix, ref Vector2 min, ref Vector2 max)
        {
            if (!mesh)
                return false;

            var b = mesh.bounds;
            for (var i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = matrix.MultiplyPoint3x4(corner);
                min = Vector2.Min(min, new Vector2(p.x, p.z));
                max = Vector2.Max(max, new Vector2(p.x, p.z));
            }
            return true;
        }
    }
}