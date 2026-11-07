// Marks where adventurers enter and leave the guild (place it inside the Guild Hall prefab, by the front door).
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Registry pattern — the newest enabled entrance is the current one (the hall prefab is re-spawned on upgrades).
    public class GuildEntrance : MonoBehaviour
    {
        public static GuildEntrance Current { get; private set; }

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(1f, 1f, 1f));
        }
    }
}