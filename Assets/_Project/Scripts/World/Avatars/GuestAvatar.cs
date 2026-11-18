// Avatars at the guild who aren't members (applicants): spawned, walked out and fixed up after room changes here; their owner
// (ApplicantAvatars) decides where they go. Never planned by the daily schedule.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class GuestAvatars
    {
        private readonly Dictionary<int, AdventurerAvatar> _guests = new();
        private readonly List<int> _ids = new();
        private readonly IGuestAvatarHost _host;

        public GuestAvatars(IGuestAvatarHost host) => _host = host;

        public int Count => _guests.Count;

        // NOTE: The concrete collection — foreach over it allocates nothing (door checks run every frame).
        public Dictionary<int, AdventurerAvatar>.ValueCollection All => _guests.Values;

        public bool Contains(int adventurerId) => _guests.ContainsKey(adventurerId);

        public bool TryGet(int adventurerId, out AdventurerAvatar avatar) =>
            _guests.TryGetValue(adventurerId, out avatar) && avatar != null;

        // A new guest standing at `at`, or at the entrance when null (no entrance → none). Null if they already have one.
        public AdventurerAvatar Spawn(Adventurer adventurer, Vector2Int? at)
        {
            if (adventurer == null || _guests.ContainsKey(adventurer.Id))
                return null;

            Vector2Int cell;
            if (at.HasValue)
                cell = at.Value;
            else if (!_host.TryGetEntrance(out cell))
                return null;

            var avatar = _host.CreateAvatar(adventurer, cell);
            if (avatar != null)
                _guests[adventurer.Id] = avatar;
            return avatar;
        }

        // Frees their spot, walks them to the entrance and removes them there; no entrance or no path = removed now.
        public void WalkOut(AdventurerAvatar avatar)
        {
            if (avatar == null)
                return;

            var id = avatar.AdventurerId;
            ActivitySpotRegistry.Release(id);
            avatar.IsLeaving = true;

            if (_host.TryGetEntrance(out var entrance) && _host.TryWalkTo(avatar, entrance, () => OnReachedDoor(id, avatar)))
                return;
            Despawn(id);
        }

        public void Despawn(int adventurerId)
        {
            ActivitySpotRegistry.Release(adventurerId);
            _host.ForgetExit(adventurerId);
            if (!_guests.Remove(adventurerId, out var avatar))
                return;

            if (avatar != null)
                Object.Destroy(avatar.gameObject);
        }

        // Takes the guest off this list without destroying it (an accepted applicant joining the members' avatars).
        public bool TryRelease(int adventurerId, out AdventurerAvatar avatar) =>
            _guests.Remove(adventurerId, out avatar) && avatar != null;

        // Adds every live guest (the sidestep snapshot).
        public void AddTo(List<AdventurerAvatar> list)
        {
            foreach (var avatar in _guests.Values)
            {
                if (avatar != null)
                    list.Add(avatar);
            }
        }

        // NOTE: After the grid rebuilt: a guest on a cell that vanished goes to the entrance (no entrance → removed); leavers
        // re-path to the door; the rest stop and free their spot — their owner sends them on again.
        public void AfterRoomsChanged(GuildNavigator navigator)
        {
            _ids.Clear();
            _ids.AddRange(_guests.Keys);
            foreach (var id in _ids)
            {
                if (!_guests.TryGetValue(id, out var avatar))
                    continue;
                if (avatar == null)
                {
                    _guests.Remove(id);
                    continue;
                }

                if (!navigator.IsWalkable(avatar.Cell))
                {
                    if (_host.TryGetEntrance(out var entrance))
                        avatar.PlaceAt(entrance);
                    else
                    {
                        Despawn(id);
                        continue;
                    }
                }

                if (avatar.IsLeaving)
                    WalkOut(avatar);
                else
                {
                    ActivitySpotRegistry.Release(id);
                    avatar.Stop();
                }
            }
        }

        // NOTE: Only if it's still this guest and still leaving — one called back, or adopted on the way, stays.
        private void OnReachedDoor(int adventurerId, AdventurerAvatar avatar)
        {
            if (avatar != null && avatar.IsLeaving && _guests.TryGetValue(adventurerId, out var current) && current == avatar)
                Despawn(adventurerId);
        }
    }
}