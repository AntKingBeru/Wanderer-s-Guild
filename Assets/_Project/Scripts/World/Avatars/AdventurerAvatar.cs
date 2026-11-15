// One adventurer in the world: walks a cell path at floor level (optionally ending on an exact point), eases aside to let
// others pass, faces a way, and sits or lies at a spot's height.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Two positions — the PATH position (how far along the route; cells, plans and arrivals use it) and the drawn
    // position = path + a small sideways offset (sidestepping). Only SidestepSolver sets the offset's target.
    public class AdventurerAvatar : MonoBehaviour
    {
        private readonly List<Vector3> _points = new();
        private AvatarSpriteView _view;
        private Grid _grid;
        private float _floorHeight;
        private int _pointIndex;
        private Action _onArrived;
        private Vector3 _pathPosition;
        private Vector3 _offset;       // NOTE: The drawn offset now (flat), eased toward _targetOffset.
        private Vector3 _targetOffset; // NOTE: Set each frame by SidestepSolver; zero = on the line.

        public int AdventurerId { get; private set; }
        public bool IsWalking => _pointIndex < _points.Count;

        // Stand / sit / sleep — only standing avatars take part in sidestepping.
        public AvatarPose Pose { get; private set; } = AvatarPose.Stand;

        // Where the route has got to (the drawn position minus the sidestep offset).
        public Vector3 PathPosition => _pathPosition;

        // True while walking out to leave (despawned on arrival).
        public bool IsLeaving { get; set; }

        // The activity the current plan was made for (a fallback still counts — no re-plan until it changes).
        public AvatarActivity PlannedFor { get; set; }

        // In-game minute after which leisure picks something new (MaxValue = hold until the schedule changes).
        public long NextDecisionAt { get; set; }

        public void Initialize(int adventurerId, AvatarSpriteView view, Grid grid, float floorHeight)
        {
            AdventurerId = adventurerId;
            _view = view;
            _grid = grid;
            _floorHeight = floorHeight;
            _pathPosition = transform.position;
        }

        // The grid cell under the route (X/Z only — sitting, lying or sidestepping doesn't change it).
        public Vector2Int Cell
        {
            get
            {
                var cell = _grid.WorldToCell(_pathPosition);
                return new Vector2Int(cell.x, cell.y);
            }
        }

        // Where the route heads now (flat, normalized; zero when standing still).
        // NOTE: Skips points already under the avatar (a path starts at its own cell's center).
        public Vector3 TravelDirection
        {
            get
            {
                for (var i = _pointIndex; i < _points.Count; i++)
                {
                    var direction = _points[i] - _pathPosition;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.0001f)
                        return direction.normalized;
                }
                return Vector3.zero;
            }
        }

        // True on the route's last leg within this distance of its end (the solver eases them back onto the line there).
        public bool IsFinishing(float within)
        {
            if (!IsWalking || _pointIndex < _points.Count - 1)
                return false;

            var toEnd = _points[^1] - _pathPosition;
            toEnd.y = 0f;
            return toEnd.sqrMagnitude <= within * within;
        }

        // The sideways offset to ease toward (flat; zero = back on the line).
        public void SetSidestep(Vector3 target)
        {
            target.y = 0f;
            _targetOffset = target;
        }

        public void PlaceAt(Vector2Int cell)
        {
            Stop();
            _pathPosition = CellCentre(cell);
            ApplyPosition();
        }

        // Walks the cells' centres in order, then (if given) the exact final point — always at floor level.
        // NOTE: Gets up first (back to the floor, standing); the arrival callback may sit or lay them down again.
        public void Walk(IReadOnlyList<Vector2Int> cells, Vector3? finalPoint, Action onArrived)
        {
            StandUp();

            _points.Clear();
            foreach (var cell in cells) _points.Add(CellCentre(cell));
            if (finalPoint.HasValue)
            {
                var p = finalPoint.Value;
                _points.Add(new Vector3(p.x, _floorHeight, p.z));
            }

            _pointIndex = 0;
            _onArrived = onArrived;
            if (_points.Count == 0)
                Arrive();
        }

        // NOTE: Stops and stands on the floor, back on the line at once (teleports via PlaceAt, canceled plans).
        public void Stop()
        {
            _points.Clear();
            _pointIndex = 0;
            _onArrived = null;
            ClearSidestep();
            _view.SetMotion(Vector3.zero);
            StandUp();
        }

        public void FaceTowards(Vector3 direction) => _view.FaceTowards(direction);

        // Stand / sit / sleep. Sitting and sleeping move the avatar to the surface height (the spot's Y: seat or mattress);
        // standing puts it back on the floor.
        public void SetPose(AvatarPose pose, float surfaceHeight)
        {
            _pathPosition.y = pose == AvatarPose.Stand ? _floorHeight : surfaceHeight;
            Pose = pose;
            ApplyPosition();
            _view.SetPose(pose);
        }

        // NOTE: Scaled deltaTime — pausing the game stops them; Fast / Very Fast speed them up (the sidestep eases alike).
        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (IsWalking)
                WalkStep(deltaTime);

            EaseOffset(deltaTime);
            ApplyPosition();
        }

        // NOTE: Moves the PATH position along the points; arriving clears the offset before the callback poses them.
        private void WalkStep(float deltaTime)
        {
            var step = GameConfig.Instance.Avatars.WalkSpeed * deltaTime;
            while (step > 0f && IsWalking)
            {
                var target = _points[_pointIndex];
                var toTarget = target - _pathPosition;
                var distance = toTarget.magnitude;

                if (distance <= step)
                {
                    _pathPosition = target;
                    step -= distance;
                    _pointIndex++;
                    continue;
                }

                _pathPosition += toTarget / distance * step;
                _view.SetMotion(toTarget);
                step = 0f;
            }

            if (!IsWalking)
                Arrive();
        }

        private void EaseOffset(float deltaTime)
        {
            if (_offset == _targetOffset)
                return;

            _offset = Vector3.MoveTowards(_offset, _targetOffset, GameConfig.Instance.AvatarSidestep.EaseSpeed * deltaTime);
        }

        private void ApplyPosition() => transform.position = _pathPosition + _offset;

        private void ClearSidestep()
        {
            _offset = Vector3.zero;
            _targetOffset = Vector3.zero;
        }

        private void StandUp() => SetPose(AvatarPose.Stand, _floorHeight);

        private void Arrive()
        {
            _points.Clear();
            _pointIndex = 0;
            ClearSidestep();
            ApplyPosition();
            _view.SetMotion(Vector3.zero);

            var callback = _onArrived;
            _onArrived = null;
            callback?.Invoke();
        }

        private Vector3 CellCentre(Vector2Int cell)
        {
            var corner = _grid.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
            return new Vector3(corner.x + 0.5f, _floorHeight, corner.z + 0.5f);
        }
    }
}