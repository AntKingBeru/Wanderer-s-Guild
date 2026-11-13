// An avatar's look: an upright billboard (yaw only) showing the walk row for its facing (a hop-and-waddle while walking if the
// sheet has no walk frames); sitting = the idle frame, legs cropped, on the seat; sleeping = laid flat, centred on the bed.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: After the camera rig (order 120), so the billboard uses this frame's final camera yaw. The avatar root is already at
    // the spot's height while sitting / sleeping (AdventurerAvatar.SetPose) — this view only adds the small pose offsets.
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
        private AvatarBobSettings _bob;
        private Camera _camera;
        private Transform _root;
        private Vector3 _lookDirection;
        private AvatarPose _pose;
        private bool _isMoving;
        private float _walkTime;
        private float _cadence;      // NOTE: This avatar's hop-rate multiplier (1 ± variation) — groups don't bob in lockstep.
        private float _width;
        private float _height;
        private int _columns;

        // NOTE: Everything below is in sheet pixels, measured per sheet — this is what makes short and tall species fit.
        private int _frameHeight;
        private float _pixel;        // world units per sheet pixel (standing)
        private int _figureTop;      // empty pixels above the head
        private int _figureBottom;   // empty pixels under the feet
        private int _figureHeight;   // the figure itself
        private float _sitCrop;      // share of the frame hidden when sitting (feet gap + legs)
        private float _sleepScale;   // this sheet's lying scale (bed-length clamp applied)

        // World point at the top of the figure's head — the label / card anchor (short species have empty headroom).
        // NOTE: From the root, not this quad — labels, bubbles and cards don't follow the walk bob.
        public Vector3 HeadTop
        {
            get
            {
                if (_settings == null || _root == null)
                    return transform.position;

                var root = _root.position;
                return _pose switch
                {
                    AvatarPose.Sleep => root + Vector3.up * _settings.SleepOffset,
                    AvatarPose.Sit => root + Vector3.up * (_settings.SitOffset + (_frameHeight * (1f - _sitCrop) - _figureTop) * _pixel),
                    _ => root + Vector3.up * ((_frameHeight - _figureTop) * _pixel)
                };
            }
        }

        // Sets the sheet and camera; sizes the quad to the frame aspect (bottom-centred on the root); works out the pose sizes.
        public void Initialize(AdventurerSpriteSheet sheet, Camera worldCamera, Transform root, AvatarSettings settings)
        {
            _renderer = GetComponent<MeshRenderer>();
            _block = new MaterialPropertyBlock();
            _sheet = sheet;
            _settings = settings;
            _camera = worldCamera;
            _root = root;

            _bob = GameConfig.Instance.AvatarBob;
            _cadence = 1f + Random.Range(-_bob.CadenceVariation, _bob.CadenceVariation);

            var frame = settings.FrameSize;
            _columns = sheet.ColumnsFor(frame);
            _frameHeight = frame.y;
            _height = settings.WorldHeight;
            _width = _height * frame.x / frame.y;
            _pixel = _height / frame.y;

            // NOTE: Clamped so a bad measurement can never leave less than a 1-pixel figure.
            _figureTop = Mathf.Clamp(sheet.FigureTop, 0, frame.y - 1);
            _figureBottom = Mathf.Clamp(sheet.FigureBottom, 0, frame.y - 1 - _figureTop);
            _figureHeight = frame.y - _figureTop - _figureBottom;

            // NOTE: Legs = a share of the FIGURE, in whole sheet pixels (no flickering sliver at the crop line).
            var cropPixels = _figureBottom + Mathf.RoundToInt(settings.SitLegShare * _figureHeight);
            _sitCrop = Mathf.Clamp(cropPixels, 0, frame.y - 1) / (float)frame.y;

            // NOTE: Tall species shrink to fit the bed; shorter ones keep the base scale.
            _sleepScale = settings.SleepScale;
            var lyingLength = _figureHeight * _pixel * _sleepScale;
            if (settings.MaxSleepLength > 0f && lyingLength > settings.MaxSleepLength)
                _sleepScale *= settings.MaxSleepLength / lyingLength;

            _block.SetTexture(BaseMapId, sheet.Texture != null ? sheet.Texture : Texture2D.whiteTexture);
            _pose = AvatarPose.Stand;
            ApplyPoseTransform();
            ApplyFrame(AvatarFacing.Down, 0, 0f);
        }

        // World-space walking direction (zero = standing still, keeping the last look direction).
        // NOTE: Moving always means standing — whoever started the walk, a sprite never slides along seated or lying.
        public void SetMotion(Vector3 direction)
        {
            _isMoving = direction.sqrMagnitude > 0.0001f;
            if (!_isMoving)
            {
                _walkTime = 0f;
                return;
            }

            _lookDirection = direction;
            SetPose(AvatarPose.Stand);
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

        // NOTE: Yaw first (the root), then the pose: a sleeper's world placement is re-set after the root turns, so it stays
        // still on the bed while the camera orbits.
        private void LateUpdate()
        {
            if (_camera == null || _sheet == null)
                return;

            var yaw = _camera.transform.eulerAngles.y;
            _root.rotation = Quaternion.Euler(0f, yaw, 0f);

            switch (_pose)
            {
                case AvatarPose.Sleep:
                    PlaceLying();
                    ApplyFrame(AvatarFacing.Down, 0, 0f);
                    return;
                case AvatarPose.Sit:
                    ApplyFrame(FacingFor(_lookDirection, yaw), 0, _sitCrop);
                    return;
            }

            if (_isMoving)
                _walkTime += Time.deltaTime;
            PlaceStanding();
            ApplyFrame(FacingFor(_lookDirection, yaw), CurrentFrame(), 0f);
        }

        // NOTE: Feet on the root; while walking a sheet that needs it, the walk bob lifts, sways and leans the quad. The lean
        // pivots at the feet: the feet-to-center offset is rotated, so the bottom edge stays at the (swayed) feet. A + Z roll
        // tips the top toward camera-left, so the lean is negated to lean toward the sway. Rebuilt every frame (never
        // accumulated) — a stop is upright and grounded at once.
        private void PlaceStanding()
        {
            var bob = _isMoving && _bob.AppliesTo(_columns > 1)
                ? AvatarWalkBob.Sample(_walkTime, _bob.StepsPerSecond * _cadence, _bob)
                : default;

            var lean = Quaternion.Euler(0f, 0f, -bob.LeanDegrees);
            transform.localRotation = lean;
            transform.localPosition = lean * new Vector3(0f, _height * 0.5f + bob.LiftPixels * _pixel, 0f) +
                                      new Vector3(bob.SwayPixels * _pixel, 0f, 0f);
        }

        // NOTE: Stand: full quad, feet on the root (floor). Sit: shorter by the crop, bottom at the root (the seat) + offset.
        // Sleep: scaled, placed by PlaceLying (now, then every frame).
        private void ApplyPoseTransform()
        {
            switch (_pose)
            {
                case AvatarPose.Sit:
                {
                    var visible = _height * (1f - _sitCrop);
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = new Vector3(_width, visible, 1f);
                    transform.localPosition = new Vector3(0f, _settings.SitOffset + visible * 0.5f, 0f);
                    break;
                }
                case AvatarPose.Sleep:
                    transform.localScale = new Vector3(_width * _sleepScale, _height * _sleepScale, 1f);
                    PlaceLying();
                    break;
                default:
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = new Vector3(_width, _height, 1f);
                    transform.localPosition = new Vector3(0f, _height * 0.5f, 0f);
                    break;
            }
        }

        // NOTE: Face up, the head along the bed's arrow, just above the root (the mattress). The quad is slid along that axis so
        // the FIGURE's center (not the frame's, which includes the headroom) sits on the spot.
        private void PlaceLying()
        {
            Vector3 head = new(_lookDirection.x, 0f, _lookDirection.z);
            if (head.sqrMagnitude < 0.0001f)
                head = _root.forward;
            head.Normalize();

            var figureCentre = _figureBottom + _figureHeight * 0.5f; // sheet px above the frame's bottom
            var shift = (_frameHeight * 0.5f - figureCentre) * _pixel * _sleepScale;
            transform.SetPositionAndRotation(_root.position + Vector3.up * _settings.SleepOffset + head * shift,
                                             Quaternion.LookRotation(Vector3.down, head));
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
        // frame from its bottom — the quad is shortened by the same share, so pixels keep their size.
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