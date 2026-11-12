// Keeps every room model's connectors showing a doorway where a finished room meets them and a wall everywhere else.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: On the FacilityManager object. Recomputes in LateUpdate after any facility change — after FacilityViews has spawned
    // the new model that frame (new models start in the prefab's default state). Runs once on load too.
    public class ConnectorStateUpdater : MonoBehaviour
    {
        [SerializeField] private Grid grid;

        private readonly HashSet<WorldDoor> _finishedDoors = new();
        private readonly List<FacilityConnector> _connectors = new();
        private bool _dirty = true;

        private void OnEnable()
        {
            GameEventsRelay.OnFacilityPlaced.AddListener(MarkDirty);
            GameEventsRelay.OnFacilityCompleted.AddListener(MarkDirty);
            GameEventsRelay.OnFacilityRemoved.AddListener(MarkDirty);
            _dirty = true;
        }

        private void OnDisable()
        {
            GameEventsRelay.OnFacilityPlaced.RemoveListener(MarkDirty);
            GameEventsRelay.OnFacilityCompleted.RemoveListener(MarkDirty);
            GameEventsRelay.OnFacilityRemoved.RemoveListener(MarkDirty);
        }

        private void MarkDirty(PlacedFacility _) => _dirty = true;

        private void LateUpdate()
        {
            if (!_dirty || grid == null || !FacilityManager.HasInstance)
                return;
            _dirty = false;

            var rooms = FacilityManager.Instance.Rooms;
            DoorLinks.CollectFinished(rooms, _finishedDoors);

            foreach (var room in rooms)
            {
                var model = FacilityManager.Instance.ModelOf(room);
                if (model == null)
                    continue;

                model.GetComponentsInChildren(true, _connectors);
                foreach (var connector in _connectors)
                {
                    var linked = room.Level > 0 &&
                                  DoorLinks.TryNearest(room, connector.transform.position, grid, out var door) &&
                                  DoorLinks.IsLinked(door, _finishedDoors);
                    connector.SetLinked(linked);
                }
            }
            _connectors.Clear();
        }
    }
}