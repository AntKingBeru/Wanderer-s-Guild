// Marks a parallax group: the camera its layers react to, how they react, their pan rest point, and an optional bridge.
using UnityEngine;

namespace WanderersGuild
{
    public class ParallaxGroup : MonoBehaviour
    {
        [Tooltip("Camera the layers react to and must cover. Defaults to Camera.main.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("The background the camera pans to/from. Repeating layers widen so both groups meet seamlessly mid-pan.")]
        [SerializeField] private ParallaxGroup bridgeTo;

        [Tooltip("Background = full pointer parallax. Character = weaker, lazier (GameConfig › Parallax).")]
        [SerializeField] private ParallaxMotion motion = ParallaxMotion.Background;

        [Tooltip("Where the camera sits when this group's layers should be undisplaced by panning. " +
                 "Empty = this object (backgrounds). Set it for groups placed inside another background, e.g. a character.")]
        [SerializeField] private Transform panOrigin;

        public Camera TargetCamera
        {
            get
            {
                if (!targetCamera)
                    targetCamera = Camera.main;
                return targetCamera;
            }
        }

        public ParallaxMotion Motion => motion;

        // Where the camera sits to see this group at rest. This object doubles as the pan anchor.
        public Vector3 Origin => transform.position;

        // The camera position at which pan parallax is zero (the pan origin, or this object).
        public Vector3 PanOrigin => panOrigin ? panOrigin.position : transform.position;

        // Horizontal distance to the bridged neighbor, or 0 when none is assigned.
        public float BridgeDistance => bridgeTo ? Mathf.Abs(bridgeTo.Origin.x - Origin.x) : 0f;
    }
}