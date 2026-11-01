// Play-Mode debug menu for facilities: build at an open door, finish construction, upgrade, demolish, log rooms and doors.
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class FacilityDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Facilities/";

        // NOTE: Stand-in for build mode (17b). Cheapest / lowest-rank first; rank, limit and gold checked once per facility;
        // positions come only from the network's open doors, lined up cell-for-cell with a door of the new room.
        [MenuItem(MenuRoot + "Build Anything Somewhere")]
        private static void BuildAnywhere()
        {
            var manager = FacilityManager.Instance;
            var rank = GuildManager.HasInstance ? GuildManager.Instance.Rank.Rank : GuildRank.F;
            var gold = GuildManager.HasInstance ? GuildManager.Instance.Wallet.Gold : 0;
            List<string> reasons = new();

            IEnumerable<FacilityData> ordered = manager.Catalog
                .OrderBy(data => data.UnlockRankFor(1))
                .ThenBy(data => data.CostFor(1));

            foreach (var data in ordered)
            {
                if (data.Id == GameConfig.Instance.Facilities.GuildHallId)
                    continue;

                var quickReason = QuickReason(data, rank, gold, manager);
                if (quickReason != null)
                {
                    reasons.Add($"{data.DisplayName}: {quickReason}");
                    continue;
                }

                if (data.Connectors.Count == 0)
                {
                    reasons.Add($"{data.DisplayName}: its level-1 prefab has no FacilityConnector markers");
                    continue;
                }

                var lastReason = "no open door it can connect to";
                foreach (var (origin, rotation) in CandidatesAtOpenDoors(data, manager))
                {
                    var check = manager.CanPlace(data, origin, rotation);
                    if (!check.Allowed)
                    {
                        lastReason = check.Reason;
                        continue;
                    }

                    var room = manager.TryPlace(data, origin, rotation);
                    if (room == null)
                        continue;

                    Debug.Log($"[Facilities] Building {data.DisplayName} #{room.Id} at {origin}, rotation {rotation * 90}°.");
                    return;
                }

                reasons.Add($"{data.DisplayName}: {lastReason}");
            }

            Debug.Log("[Facilities] Nothing could be built:\n" + string.Join("\n", reasons));
        }

        [MenuItem(MenuRoot + "Log Connectors")]
        private static void LogConnectors()
        {
            var manager = FacilityManager.Instance;
            foreach (var room in manager.Rooms)
            {
                if (room.Doors.Count == 0)
                {
                    Debug.LogWarning($"#{room.Id} {room.Data.DisplayName}: NO doors (check FacilityConnector markers on its level-1 prefab).");
                    continue;
                }

                foreach (var door in room.Doors)
                {
                    var outside = door.Partner;
                    Debug.Log($"#{room.Id} {room.Data.DisplayName}: door {door.A}+{door.B} facing {door.Facing} → " +
                              $"{outside.A}+{outside.B} ({(manager.Grid.IsOpen(door) ? "open" : "occupied")})");
                }
            }
        }

        [MenuItem(MenuRoot + "Finish Construction Now")]
        private static void FinishAll() => FacilityManager.Instance.FinishAllNow();

        [MenuItem(MenuRoot + "Upgrade First Upgradable")]
        private static void UpgradeFirst()
        {
            foreach (var room in FacilityManager.Instance.Rooms)
            {
                if (FacilityManager.Instance.TryUpgrade(room))
                {
                    Debug.Log($"[Facilities] Upgrading {room.Data.DisplayName} #{room.Id} to level {room.TargetLevel}.");
                    return;
                }
            }
            Debug.Log("[Facilities] Nothing could be upgraded.");
        }

        [MenuItem(MenuRoot + "Demolish Newest")]
        private static void DemolishNewest()
        {
            var rooms = FacilityManager.Instance.Rooms;
            for (var i = rooms.Count - 1; i >= 0; i--)
            {
                var check = FacilityManager.Instance.CanDemolish(rooms[i]);
                if (!check.Allowed)
                    continue;

                var room = rooms[i];
                FacilityManager.Instance.TryDemolish(room);
                Debug.Log($"[Facilities] Demolished {room.Data.DisplayName} #{room.Id} (+{check.Gold}g).");
                return;
            }
            Debug.Log("[Facilities] Nothing can be demolished.");
        }

        [MenuItem(MenuRoot + "Log Facilities")]
        private static void LogRooms()
        {
            foreach (var room in FacilityManager.Instance.Rooms)
            {
                Debug.Log($"#{room.Id} {room.Data.DisplayName} — level {room.Level}/{room.Data.MaxLevel}, {room.State}, " +
                          $"origin {room.Origin}, rotation {room.Rotation * 90}°, spent {room.TotalSpent}g");
            }
        }

        [MenuItem(MenuRoot + "Build Anything Somewhere", true)]
        [MenuItem(MenuRoot + "Log Connectors", true)]
        [MenuItem(MenuRoot + "Finish Construction Now", true)]
        [MenuItem(MenuRoot + "Upgrade First Upgradable", true)]
        [MenuItem(MenuRoot + "Demolish Newest", true)]
        [MenuItem(MenuRoot + "Log Facilities", true)]
        private static bool CanUse() => EditorApplication.isPlaying && FacilityManager.HasInstance;

        // NOTE: Position-independent checks — failing any of these makes every position fail the same way.
        private static string QuickReason(FacilityData data, GuildRank rank, long gold, FacilityManager manager)
        {
            var unlock = data.UnlockRankFor(1);
            if (unlock > rank)
                return $"needs guild rank {unlock}";
            if (data.Limit > 0 && FacilityRules.CountOf(data, manager.Rooms) >= data.Limit)
                return $"limit reached ({data.Limit})";
            return gold < data.CostFor(1) ? $"costs {data.CostFor(1):N0}g (have {gold:N0}g)" : null;
        }

        // NOTE: For each open door, every rotation and door of the new room whose rotated door lands exactly on that door's two
        // outside cells, facing back. Doors are kept in a fixed cell order, so A lines up with A and B with B.
        private static IEnumerable<(Vector2Int origin, int rotation)> CandidatesAtOpenDoors(FacilityData data, FacilityManager manager)
        {
            foreach (var room in manager.Rooms)
            {
                foreach (var door in room.Doors)
                {
                    if (!manager.Grid.IsOpen(door))
                        continue;

                    var target = door.Partner;
                    for (var rotation = 0; rotation < 4; rotation++)
                    {
                        foreach (var connector in data.Connectors)
                        {
                            var local = FacilityFootprint.ToWorldDoor(connector, data.Size, Vector2Int.zero, rotation);
                            if (local.Facing == target.Facing)
                                yield return (target.A - local.A, rotation);
                        }
                    }
                }
            }
        }
    }
}