// Builds an avatar GameObject (root with AdventurerAvatar + AvatarInteractable; lit quad with AvatarSpriteView; a thin click
// hitbox child on the Interactable layer) using the species + class sheet.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WanderersGuild
{
    public static class AvatarFactory
    {
        private static readonly HashSet<AdventurerSpriteSheet> Warned = new();

        // NOTE: Only the hitbox goes on the Interactable layer — never the renderer (that layer may be culled by the camera,
        // like furniture, whose renderers also keep their own layers).
        public static AdventurerAvatar Create(Adventurer adventurer, Transform parent, Camera worldCamera, Grid grid,
                                              AvatarSettings settings)
        {
            GameObject root = new($"Avatar · {adventurer.Name}");
            root.transform.SetParent(parent, false);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.name = "Sprite";
            quad.transform.SetParent(root.transform, false);

            var renderer = quad.GetComponent<MeshRenderer>();
            if (settings.Material)
                renderer.sharedMaterial = settings.Material;
            else
                Debug.LogWarning("[AvatarFactory] GameConfig › Avatars › Material is empty — avatars use the default material.");
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            var view = quad.AddComponent<AvatarSpriteView>();
            view.Initialize(SheetFor(adventurer, settings), worldCamera, root.transform, settings);

            AddHitbox(quad.transform);

            var avatar = root.AddComponent<AdventurerAvatar>();
            avatar.Initialize(adventurer.Id, view, grid, settings.FloorHeight);

            var interactable = root.AddComponent<AvatarInteractable>();
            interactable.Initialize(adventurer.Id, adventurer.Name, renderer);
            return avatar;
        }

        // NOTE: A child of the quad, so it inherits its camera-facing yaw and its size (1×1 local = the visible sprite);
        // thin, so it doesn't catch rays meant for things behind it at grazing angles.
        private static void AddHitbox(Transform quad)
        {
            GameObject hitbox = new("Hitbox") { layer = GameConfig.Instance.Interaction.InteractableLayer };
            hitbox.transform.SetParent(quad, false);

            var box = hitbox.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 0.05f);
        }

        // NOTE: Catalog (most specific) → its default → generated placeholder. A sheet that isn't whole frames is warned
        // once and replaced by the placeholder, so a bad import never shows sliced frames.
        private static AdventurerSpriteSheet SheetFor(Adventurer adventurer, AvatarSettings settings)
        {
            var sheet = settings.Catalog ? settings.Catalog.Find(adventurer.Species, adventurer.Class) : null;
            var problem = string.Empty;
            if (sheet && sheet.IsValid(settings.FrameSize, out problem)) return sheet;

            if (sheet && Warned.Add(sheet))
                Debug.LogWarning($"[AvatarFactory] Sprite sheet '{sheet.name}' {problem}; using a placeholder.", sheet);

            return PlaceholderAvatarSheets.For(adventurer.Species, adventurer.Class, settings.FrameSize);
        }

        // Clears the once-per-sheet warnings (scene teardown).
        public static void ResetWarnings() => Warned.Clear();
    }
}