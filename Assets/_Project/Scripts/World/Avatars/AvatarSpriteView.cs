// An avatar's look: an upright billboard (yaw only) showing the walk row for its facing; sitting = the idle frame with the legs
// cropped, lowered onto the seat; sleeping = the idle frame laid flat on the bed (head toward the spot's arrow).
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: After the camera rig (order 120), so the billboard uses this frame's final camera yaw.
    [DefaultExecutionOrder(120)]
    [RequireComponent(typeof(MeshRenderer))]
    public class AvatarSpriteView : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;
        private AdventurerSpriteSheet _sheet;
        private AvatarSettings _settings;
        private Camera _camera;
        private Transform _root;
        private Vector3 _lookDirection;
        private AvatarPose _pose;
        private bool _isMoving;
        private float _walkTime;
        private float _width;
        private float _height;
        private float _crop;
        private int _columns;

        // Sets the sheet and camera; sizes the quad to the frame aspect (bottom-centred on the root).
        public void Initialize(AdventurerSpriteSheet sheet, Camera worldCamera, Transform root, AvatarSettings settings)
        {
            _renderer = GetComponent<MeshRenderer>();
            _block = new MaterialPropertyBlock();
            _sheet = sheet;
            _settings = settings;
            _camera = worldCamera;
            _root = root;

            var frame = settings.FrameSize;
            _columns = sheet.ColumnsFor(frame);

            _height = settings.WorldHeight;
            _width = _height * frame.x / frame.y;

            // NOTE: Whole sheet pixels only — a fractional crop would leave a flickering sliver of legs.
            _crop = Mathf.Round(settings.SitCrop * frame.y) / frame.y;

            _block.SetTexture(BaseMapId, sheet.Texture ?? Texture2D.whiteTexture);
            _pose = AvatarPose.Stand;
            ApplyPoseTransform();
            ApplyFrame(AvatarFacing.Down, 0, 0f);
        }

        // World-space walking direction (zero = standing, keeping the last look direction).
        public void SetMotion(Vector3 direction)
        {
            _isMoving = direction.sqrMagnitude > 0.0001f;
            if (_isMoving)
                _lookDirection = direction;
            else
                _walkTime = 0f;
        }

        // Stands still looking this way (world space) — e.g. a spot's arrow (for a bed: toward the pillow).
        public void FaceTowards(Vector3 direction)
        {
            _isMoving = false;
            _walkTime = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                _lookDirection = direction;
        }

        // Stand / sit / sleep — re-applies size, position and rotation in one place.
        public void SetPose(AvatarPose pose)
        {
            if (pose == _pose)
                return;

            _pose = pose;
            ApplyPoseTransform();
        }

        // NOTE: Yaw first (the root), then the pose: a sleeper's world rotation is re-set after the root turns, so it stays
        // still on the bed while the camera orbits.
        private void LateUpdate()
        {
            if (_camera is null || _sheet is null)
                return;

            var yaw = _camera.transform.eulerAngles.y;
            _root.rotation = Quaternion.Euler(0f, yaw, 0f);

            switch (_pose)
            {
                case AvatarPose.Sleep:
                    transform.rotation = LyingRotation();
                    ApplyFrame(AvatarFacing.Down, 0, 0f);
                    return;
                case AvatarPose.Sit:
                    ApplyFrame(FacingFor(_lookDirection, yaw), 0, _crop);
                    return;
            }

            if (_isMoving)
                _walkTime += Time.deltaTime;
            ApplyFrame(FacingFor(_lookDirection, yaw), CurrentFrame(), 0f);
        }

        // NOTE: Face up (normal along world Y — both faces render), sprite "up" (the head) along the bed's arrow.
        private Quaternion LyingRotation()
        {
            Vector3 head = new(_lookDirection.x, 0f, _lookDirection.z);
            if (head.sqrMagnitude < 0.0001f)
                head = _root.forward;
            return Quaternion.LookRotation(Vector3.down, head.normalized);
        }

        // NOTE: Stand: full quad, bottom on the floor. Sit: shorter by the crop, bottom at the seat. Sleep: scaled, centred on the
        // bed at mattress height (rotation set each frame in LateUpdate).
        private void ApplyPoseTransform()
        {
            switch (_pose)
            {
                case AvatarPose.Sit:
                {
                    float visible = _height * (1f - _crop);
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = new Vector3(_width, visible, 1f);
                    transform.localPosition = new Vector3(0f, _settings.SitLift + visible * 0.5f, 0f);
                    break;
                }
                case AvatarPose.Sleep:
                {
                    var scale = _settings.SleepScale;
                    transform.localScale = new Vector3(_width * scale, _height * scale, 1f);
                    transform.localPosition = new Vector3(0f, _settings.SleepLift, 0f);
                    break;
                }
                default:
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = new Vector3(_width, _height, 1f);
                    transform.localPosition = new Vector3(0f, _height * 0.5f, 0f);
                    break;
            }
        }

        // NOTE: The direction is turned into camera space: toward the camera = Down, away = Up.
        private static AvatarFacing FacingFor(Vector3 direction, float cameraYaw)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return AvatarFacing.Down;

            var local = Quaternion.Euler(0f, -cameraYaw, 0f) * direction;
            if (Mathf.Abs(local.x) > Mathf.Abs(local.z))
                return local.x > 0f ? AvatarFacing.Right : AvatarFacing.Left;
            return local.z > 0f ? AvatarFacing.Up : AvatarFacing.Down;
        }

        private int CurrentFrame()
        {
            if (!_isMoving || _columns <= 1)
                return 0;

            var walkFrames = _columns - 1;
            return 1 + Mathf.FloorToInt(_walkTime * _sheet.WalkFramesPerSecond) % walkFrames;
        }

        // NOTE: UV scale/offset for one frame (texture rows bottom-up, sheet rows top-down); cropBottom hides that share of the
        // frame from its bottom (the legs) — the quad is shortened by the same share, so pixels keep their size.
        private void ApplyFrame(AvatarFacing facing, int column, float cropBottom)
        {
            float cols = _columns;
            const float rows = AdventurerSpriteSheet.Rows;
            var frameBottom = (rows - 1 - (int)facing) / rows;
            Vector4 st = new(1f / cols, (1f - cropBottom) / rows, column / cols, frameBottom + cropBottom / rows);
            _block.SetVector(BaseMapStId, st);
            _renderer.SetPropertyBlock(_block);
        }
    }
}