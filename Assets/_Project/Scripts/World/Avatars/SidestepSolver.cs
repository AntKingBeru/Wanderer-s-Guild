// Lets walkers pass: two coming head-on ease to opposite sides; someone standing in the way is walked around. Never into
// furniture or another room, and never for anyone sitting, lying, standing still or about to reach their spot.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Run once a frame by AvatarManager over every avatar (≤ 48, so O(n²) is cheap). Only sets each walker's TARGET
    // offset; the avatar eases toward it. PATH positions are compared, so someone stays "in the way" until passed even after
    // both have eased aside. Head-on pairs are solved together, so they always split to opposite sides.
    public sealed class SidestepSolver
    {
        private const float OpposingDot = -0.5f;    // NOTE: Directions more opposite than this (≈120°+) count as head-on.
        private const float CentredLateral = 0.05f; // NOTE: Closer than this to the line = dead ahead (no side preferred).
        private const float AheadProbe = 0.5f;      // NOTE: The offset is also checked this far further along the route.

        private readonly List<SidestepAgent> _agents = new();
        private readonly List<int> _blockers = new(); // NOTE: Per agent: the index of who's in its way (−1 = nobody).
        private readonly Func<Vector3, Vector2Int> _cellOf;

        public SidestepSolver(Func<Vector3, Vector2Int> cellOf) => _cellOf = cellOf;

        public void Solve(IReadOnlyList<AdventurerAvatar> avatars, GuildNavigator navigator, AvatarSidestepSettings settings)
        {
            Collect(avatars);
            if (!settings.Enabled)
            {
                foreach (var agent in _agents)
                    agent.Avatar.SetSidestep(Vector3.zero);
                return;
            }

            _blockers.Clear();
            for (var i = 0; i < _agents.Count; i++)
                _blockers.Add(FindBlocker(i, settings));

            for (var i = 0; i < _agents.Count; i++)
                Resolve(i, navigator, settings);
        }

        // NOTE: Only standing avatars (walking or still); sitters and sleepers are on furniture, off every route.
        private void Collect(IReadOnlyList<AdventurerAvatar> avatars)
        {
            _agents.Clear();
            foreach (var avatar in avatars)
            {
                if (avatar == null || avatar.Pose != AvatarPose.Stand)
                    continue;
                _agents.Add(new SidestepAgent(avatar, avatar.PathPosition, avatar.TravelDirection));
            }
        }

        // The nearest one in this walker's way: head-on walkers or anyone standing still, ahead (or still alongside), near
        // its line. Same-way and crossing walkers are ignored.
        private int FindBlocker(int index, AvatarSidestepSettings settings)
        {
            var walker = _agents[index];
            if (!walker.IsWalking)
                return -1;

            var best = -1;
            var bestAhead = float.MaxValue;
            for (var j = 0; j < _agents.Count; j++)
            {
                if (j == index)
                    continue;

                var other = _agents[j];
                if (other.IsWalking && Vector3.Dot(other.Direction, walker.Direction) > OpposingDot)
                    continue;

                var relative = other.Position - walker.Position;
                relative.y = 0f;
                var ahead = Vector3.Dot(relative, walker.Direction);
                if (ahead <= -settings.KeepBehind || ahead >= settings.LookAhead)
                    continue;
                if (Mathf.Abs(Vector3.Dot(relative, walker.Right)) >= settings.PassWidth)
                    continue;

                if (ahead < bestAhead)
                {
                    bestAhead = ahead;
                    best = j;
                }
            }
            return best;
        }

        // NOTE: A mutual head-on pair is solved once, by its lower index; anyone else steps around their blocker alone.
        private void Resolve(int index, GuildNavigator navigator, AvatarSidestepSettings settings)
        {
            var walker = _agents[index];
            if (!walker.IsWalking)
                return;

            var blocker = _blockers[index];
            if (blocker < 0 || IsSettling(walker, settings))
            {
                walker.Avatar.SetSidestep(Vector3.zero);
                return;
            }

            if (_blockers[blocker] == index)
            {
                if (index < blocker)
                    SolvePair(walker, _agents[blocker], navigator, settings);
                return;
            }

            SolveAround(walker, _agents[blocker], navigator, settings);
        }

        // NOTE: Away from the one in the way (dead ahead → right), else the other side, else stay on the line.
        private void SolveAround(SidestepAgent walker, SidestepAgent other, GuildNavigator navigator, AvatarSidestepSettings settings)
        {
            var lateral = Vector3.Dot(other.Position - walker.Position, walker.Right);
            var side = lateral > CentredLateral ? -1f : 1f;
            var step = walker.Right * settings.SideDistance;

            if (CanOffset(walker, step * side, navigator, settings))
                walker.Avatar.SetSidestep(step * side);
            else if (CanOffset(walker, -step * side, navigator, settings))
                walker.Avatar.SetSidestep(-step * side);
            else
                walker.Avatar.SetSidestep(Vector3.zero);
        }

        // NOTE: Both on opposite world sides (each to their own right first — or left if B is already on A's right); if no split
        // fits, whoever has room steps aside alone; else both stay on the line (they pass through, as before).
        private void SolvePair(SidestepAgent a, SidestepAgent b, GuildNavigator navigator, AvatarSidestepSettings settings)
        {
            var axis = a.Right * settings.SideDistance;
            var aMoves = !IsSettling(a, settings);
            var bMoves = !IsSettling(b, settings);
            var first = Vector3.Dot(b.Position - a.Position, a.Right) > CentredLateral ? -1f : 1f;

            for (var attempt = 0; attempt < 2; attempt++)
            {
                var sign = attempt == 0 ? first : -first;
                var aOffset = aMoves ? axis * sign : Vector3.zero;
                var bOffset = bMoves ? -axis * sign : Vector3.zero;
                if (CanOffset(a, aOffset, navigator, settings) && CanOffset(b, bOffset, navigator, settings))
                {
                    a.Avatar.SetSidestep(aOffset);
                    b.Avatar.SetSidestep(bOffset);
                    return;
                }
            }

            for (var attempt = 0; attempt < 2; attempt++)
            {
                var sign = attempt == 0 ? first : -first;
                if (aMoves && CanOffset(a, axis * sign, navigator, settings))
                {
                    a.Avatar.SetSidestep(axis * sign);
                    b.Avatar.SetSidestep(Vector3.zero);
                    return;
                }
                if (bMoves && CanOffset(b, -axis * sign, navigator, settings))
                {
                    a.Avatar.SetSidestep(Vector3.zero);
                    b.Avatar.SetSidestep(-axis * sign);
                    return;
                }
            }

            a.Avatar.SetSidestep(Vector3.zero);
            b.Avatar.SetSidestep(Vector3.zero);
        }

        private static bool IsSettling(SidestepAgent agent, AvatarSidestepSettings settings) =>
            agent.Avatar.IsFinishing(settings.SettleDistance);

        // NOTE: The offset + clearance must land in a free cell of the same room as the route — here and a little further on
        // (a doorway compares each point with its own side's room). Zero always fits.
        private bool CanOffset(SidestepAgent agent, Vector3 offset, GuildNavigator navigator, AvatarSidestepSettings settings)
        {
            if (offset.sqrMagnitude < 0.0001f)
                return true;

            var reach = offset + offset.normalized * settings.Clearance;
            return IsClear(agent.Position, reach, navigator) &&
                   IsClear(agent.Position + agent.Direction * AheadProbe, reach, navigator);
        }

        private bool IsClear(Vector3 linePoint, Vector3 reach, GuildNavigator navigator)
        {
            var lineCell = _cellOf(linePoint);
            var cell = _cellOf(linePoint + reach);
            return navigator.IsFree(cell) && navigator.RoomOf(cell) == navigator.RoomOf(lineCell);
        }
    }
}