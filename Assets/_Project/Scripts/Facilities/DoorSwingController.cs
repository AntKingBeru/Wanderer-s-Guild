// Opens every door leaf while an adventurer is near it and closes it when they've passed (flat distance, every frame).
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: On the AvatarManager object. Positions are gathered once per frame into a reused list — no allocations.
    public class DoorSwingController : MonoBehaviour
    {
        private readonly List<Vector2> _positions = new();

        private void LateUpdate()
        {
            var leaves = DoorLeaf.All;
            if (leaves.Count == 0)
                return;

            GatherAvatarPositions();

            var settings = GameConfig.Instance.Doors;
            var radiusSq = settings.OpenRadius * settings.OpenRadius;
            var dt = Time.deltaTime;

            for (var i = leaves.Count - 1; i >= 0; i--)
            {
                var leaf = leaves[i];
                var sense = leaf.SensePosition;
                Vector2 point = new(sense.x, sense.z);

                var someoneNear = _positions.Any(position => !((position - point).sqrMagnitude > radiusSq));
                leaf.Step(someoneNear, settings.SwingSpeed, dt);
            }
        }

        private void GatherAvatarPositions()
        {
            _positions.Clear();
            if (!AvatarManager.HasInstance)
                return;

            foreach (var avatar in AvatarManager.Instance.Avatars.Values)
            {
                if (avatar == null)
                    continue;
                var p = avatar.transform.position;
                _positions.Add(new Vector2(p.x, p.z));
            }
        }
    }
}