// An avatar's look: an upright billboard (turns around the vertical axis only) showing the sheet row for its facing.
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
        private Camera _camera;
        private Transform _root;
        private Vector3 _lookDirection;
        private bool _isMoving;
        private float _walkTime;
        private int _columns;

        // Sets the sheet and camera; sizes the quad to the frame aspect (bottom-centred on the root).
        public void Initialize(AdventurerSpriteSheet sheet, Camera worldCamera, Transform root, AvatarSettings settings)
        {
            _renderer = GetComponent<MeshRenderer>();
            _block = new MaterialPropertyBlock();
            _sheet = sheet;
            _camera = worldCamera;
            _root = root;

            var frame = settings.FrameSize;
            _columns = sheet.ColumnsFor(frame);

            var height = settings.WorldHeight;
            var width = height * frame.x / frame.y;
            transform.localScale = new Vector3(width, height, 1f);
            transform.localPosition = new Vector3(0f, height * 0.5f, 0f);

            _block.SetTexture(BaseMapId, sheet.Texture ? sheet.Texture : Texture2D.whiteTexture);
            ApplyFrame(AvatarFacing.Down, 0);
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

        // Stands still looking this way (world space) — e.g. a spot's arrow.
        public void FaceTowards(Vector3 direction)
        {
            _isMoving = false;
            _walkTime = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                _lookDirection = direction;
        }

        // NOTE: Yaw only — the quad stays perpendicular to the floor whatever the camera's pitch.
        private void LateUpdate()
        {
            if (!_camera || !_sheet)
                return;

            var yaw = _camera.transform.eulerAngles.y;
            _root.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (_isMoving)
                _walkTime += Time.deltaTime;
            ApplyFrame(FacingFor(_lookDirection, yaw), CurrentFrame());
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

        // NOTE: UV scale/offset for one frame; texture rows are bottom-up, sheet rows top-down.
        private void ApplyFrame(AvatarFacing facing, int column)
        {
            float cols = _columns;
            const float rows = AdventurerSpriteSheet.Rows;
            Vector4 st = new(1f / cols, 1f / rows, column / cols, (rows - 1 - (int)facing) / rows);
            _block.SetVector(BaseMapStId, st);
            _renderer.SetPropertyBlock(_block);
        }
    }
}