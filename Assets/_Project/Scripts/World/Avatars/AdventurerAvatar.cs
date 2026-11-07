// One adventurer in the world: walks a cell path (optionally ending on an exact point), faces a way, knows its plan.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public class AdventurerAvatar : MonoBehaviour
    {
        private readonly List<Vector3> _points = new();
        private AvatarSpriteView _view;
        private Grid _grid;
        private float _floorHeight;
        private int _pointIndex;
        private Action _onArrived;

        public int AdventurerId { get; private set; }
        public bool IsWalking => _pointIndex < _points.Count;

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
        }

        // The grid cell under the avatar.
        public Vector2Int Cell
        {
            get
            {
                var cell = _grid.WorldToCell(transform.position);
                return new Vector2Int(cell.x, cell.y);
            }
        }

        public void PlaceAt(Vector2Int cell)
        {
            Stop();
            transform.position = CellCentre(cell);
        }

        // Walks the cells' centres in order, then (if given) the exact final point.
        public void Walk(IReadOnlyList<Vector2Int> cells, Vector3? finalPoint, Action onArrived)
        {
            _points.Clear();
            foreach (var cell in cells) _points.Add(CellCentre(cell));
            if (finalPoint.HasValue)
            {
                var p = finalPoint.Value;
                _points.Add(new Vector3(p.x, _floorHeight, p.z));
            }

            _pointIndex = 0;
            _onArrived = onArrived;
            if (_points.Count == 0) Arrive();
        }

        public void Stop()
        {
            _points.Clear();
            _pointIndex = 0;
            _onArrived = null;
            _view.SetMotion(Vector3.zero);
        }

        public void FaceTowards(Vector3 direction) => _view.FaceTowards(direction);

        // NOTE: Scaled deltaTime — pausing the game stops them; Fast / Very Fast speed them up.
        private void Update()
        {
            if (!IsWalking)
                return;

            var step = GameConfig.Instance.Avatars.WalkSpeed * Time.deltaTime;
            while (step > 0f && IsWalking)
            {
                var target = _points[_pointIndex];
                var toTarget = target - transform.position;
                var distance = toTarget.magnitude;

                if (distance <= step)
                {
                    transform.position = target;
                    step -= distance;
                    _pointIndex++;
                    continue;
                }

                transform.position += toTarget / distance * step;
                _view.SetMotion(toTarget);
                step = 0f;
            }

            if (!IsWalking)
                Arrive();
        }

        private void Arrive()
        {
            _points.Clear();
            _pointIndex = 0;
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