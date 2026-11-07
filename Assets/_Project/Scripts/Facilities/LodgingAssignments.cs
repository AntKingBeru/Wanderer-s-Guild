// Who sleeps where (pure state): adventurer → room id (0 = outside); counts per room; save/restore.
using System.Linq;
using System.Collections.Generic;

namespace WanderersGuild
{
    public sealed class LodgingAssignments
    {
        public const int Outside = 0;

        private readonly Dictionary<int, int> _roomOf = new();

        public int OutsideCount
        {
            get
            {
                return _roomOf.Values.Count(room => room == Outside);
            }
        }

        public bool Has(int adventurerId) => _roomOf.ContainsKey(adventurerId);

        // Their room id (0 = outside), or −1 if they aren't housed.
        public int RoomOf(int adventurerId) => _roomOf.GetValueOrDefault(adventurerId, -1);

        public int CountIn(int roomId)
        {
            return _roomOf.Values.Count(room => room == roomId);
        }

        // Adventurer ids lodged in this room (0 = outside), filled into <paramref name="results"/>.
        public void ResidentsOf(int roomId, List<int> results)
        {
            results.Clear();
            results.AddRange(from pair in _roomOf where pair.Value == roomId select pair.Key);
        }

        public void Assign(int adventurerId, int roomId) => _roomOf[adventurerId] = roomId;
        public bool Remove(int adventurerId) => _roomOf.Remove(adventurerId);

        public List<SavedLodging> ToSaved()
        {
            return _roomOf.Select(pair => new SavedLodging(pair.Key, pair.Value)).ToList();
        }
    }
}