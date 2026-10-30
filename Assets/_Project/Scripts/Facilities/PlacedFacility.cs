// A room in the guild: its facility, grid origin and rotation, level, construction state, and its world cells/connectors.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class PlacedFacility
    {
        private readonly List<Vector2Int> _cells = new();
        private readonly List<(Vector2Int cell, CardinalDirection facing)> _connectors = new();

        public PlacedFacility(int id, FacilityData data, Vector2Int origin, int rotation, int level, int targetLevel,
                              FacilityState state, long finishAtMinutes, long totalSpent)
        {
            Id = id;
            Data = data;
            Origin = origin;
            Rotation = rotation & 3;
            Level = level;
            TargetLevel = targetLevel;
            State = state;
            FinishAtMinutes = finishAtMinutes;
            TotalSpent = totalSpent;

            _cells.AddRange(FacilityFootprint.Cells(origin, data.Size, Rotation));
            foreach (var connector in data.Connectors)
            {
                _connectors.Add((FacilityFootprint.ToWorldCell(connector.Cell, data.Size, origin, Rotation),
                                 FacilityFootprint.Rotate(connector.Facing, Rotation)));
            }
        }

        public int Id { get; }
        public FacilityData Data { get; }
        public Vector2Int Origin { get; }
        public int Rotation { get; }

        // Finished level (0 while a new room is still being built — it has no effect then).
        public int Level { get; private set; }

        // The level being built towards (equals Level when complete).
        public int TargetLevel { get; private set; }
        public FacilityState State { get; private set; }
        public long FinishAtMinutes { get; private set; }

        // Everything paid for this room so far (the demolish refund is a share of it).
        public long TotalSpent { get; private set; }

        public IReadOnlyList<Vector2Int> Cells => _cells;
        public IReadOnlyList<(Vector2Int cell, CardinalDirection facing)> Connectors => _connectors;
        public bool IsUnderConstruction => State != FacilityState.Complete;

        public void StartUpgrade(long cost, long finishAtMinutes)
        {
            TargetLevel = Level + 1;
            State = FacilityState.Upgrading;
            FinishAtMinutes = finishAtMinutes;
            TotalSpent += cost;
        }

        public void FinishConstruction()
        {
            Level = TargetLevel;
            State = FacilityState.Complete;
            FinishAtMinutes = 0;
        }

        public SavedFacility ToSaved() =>
            new(Id, Data.Id, Origin, Rotation, Level, TargetLevel, State, FinishAtMinutes, TotalSpent);
    }
}