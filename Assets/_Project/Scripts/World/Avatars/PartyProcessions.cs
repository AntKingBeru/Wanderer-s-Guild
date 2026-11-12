// Quest parties leaving together: members gather beside the entrance, stand together a moment, then walk out one after
// another a short gap apart. Solo departures (and layouts with no entrance) use the normal walk-out.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: A state machine per party (Starting → Gathering → Huddling → Filing), ticked by AvatarManager with scaled time —
    // pausing freezes it and Fast speed hurries it, like the walking.
    public sealed class PartyProcessions
    {
        private readonly IProcessionHost _host;
        private readonly Dictionary<int, PartyProcession> _byQuest = new();
        private readonly List<int> _finished = new();
        private readonly List<Vector2Int> _cells = new();
        private readonly List<Vector2Int> _neighbours = new();
        private readonly Queue<Vector2Int> _frontier = new();
        private readonly HashSet<Vector2Int> _visited = new();

        public PartyProcessions(IProcessionHost host) => _host = host;

        private static ProcessionSettings Settings => GameConfig.Instance.Processions;

        // NOTE: Only for a quest departure of a party of 2+, while that party hasn't started gathering. The avatar stops where
        // it is, its spot is freed, and it's marked leaving (the schedule skips it); the next Tick sends everyone to gather.
        // False = leave the normal way.
        public bool TryJoin(AdventurerAvatar avatar, Adventurer adventurer)
        {
            if (avatar == null || adventurer is not { Status: AdventurerStatus.OnQuest } || !_host.TryGetEntrance(out _))
                return false;
            if (!TryFindDepartingQuest(adventurer.Id, out var quest) || quest.MemberIds.Count < 2)
                return false;

            if (!_byQuest.TryGetValue(quest.Id, out var procession))
                _byQuest[quest.Id] = procession = new PartyProcession();
            if (procession.Phase != ProcessionPhase.Starting)
                return false;

            ActivitySpotRegistry.Release(avatar.AdventurerId);
            avatar.Stop();
            avatar.IsLeaving = true;
            procession.Add(avatar);
            return true;
        }

        // Drops every procession (facility changes: the owner re-paths every leaver to the door itself).
        public void Clear() => _byQuest.Clear();

        public void Tick(float deltaTime)
        {
            if (_byQuest.Count == 0)
                return;

            _finished.Clear();
            foreach (var (questId, procession) in _byQuest)
            {
                if (Advance(procession, deltaTime))
                    _finished.Add(questId);
            }

            foreach (var questId in _finished)
                _byQuest.Remove(questId);
        }

        // NOTE: True when it's over (everyone sent, or nobody left).
        private bool Advance(PartyProcession procession, float deltaTime)
        {
            procession.DropInvalid();
            if (procession.Count == 0)
                return true;

            var settings = Settings;
            switch (procession.Phase)
            {
                case ProcessionPhase.Starting:
                    if (procession.Count == 1 || !_host.TryGetEntrance(out var entrance))
                    {
                        SendAll(procession);
                        return true;
                    }
                    StartGathering(procession, entrance);
                    return false;

                case ProcessionPhase.Gathering:
                    procession.Timer += deltaTime;
                    if (procession.AllGathered() || procession.Timer >= settings.MaxGatherSeconds)
                        Enter(procession, ProcessionPhase.Huddling, 0f);
                    return false;

                case ProcessionPhase.Huddling:
                    procession.Timer += deltaTime;
                    if (procession.Timer >= settings.HuddleSeconds)
                        Enter(procession, ProcessionPhase.Filing, settings.FileGapSeconds); // NOTE: The first goes at once.
                    return false;

                case ProcessionPhase.Filing:
                    procession.Timer += deltaTime;
                    while (procession.Timer >= settings.FileGapSeconds)
                    {
                        var next = procession.NextToSend();
                        if (next == null)
                            return true;
                        procession.Timer -= settings.FileGapSeconds;
                        Send(next);
                    }
                    return procession.NextToSend() == null;

                default:
                    return true;
            }
        }

        // NOTE: Member "i" walks to the i-th nearest cell; with no cell or no path it waits where it stands.
        private void StartGathering(PartyProcession procession, Vector2Int entrance)
        {
            FindGatherCells(entrance, procession.Count);
            var door = _host.CellCentre(entrance);

            for (var i = 0; i < procession.Count; i++)
            {
                var member = procession.Members[i];
                if (i < _cells.Count && _host.TryWalkTo(member.Avatar, _cells[i], () => OnGathered(member, door)))
                    continue;
                member.Gathered = true;
            }

            Enter(procession, ProcessionPhase.Gathering, 0f);
        }

        // NOTE: Faces the door while waiting (a stale callback after a reset is harmless — it only sets flags and facing).
        private static void OnGathered(PartyProcession.Member member, Vector3 door)
        {
            member.Gathered = true;
            if (member.Avatar == null)
                return;

            var direction = door - member.Avatar.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                member.Avatar.FaceTowards(direction);
        }

        // NOTE: Breadth-first from the entrance through real steps (Neighbors respects walls and doors), inside the
        // entrance's room only, skipping the entrance cell itself — nearest first.
        private void FindGatherCells(Vector2Int entrance, int count)
        {
            _cells.Clear();
            _visited.Clear();
            _frontier.Clear();

            var nav = _host.Nav;
            var room = nav.RoomOf(entrance);
            _frontier.Enqueue(entrance);
            _visited.Add(entrance);

            while (_frontier.Count > 0 && _cells.Count < count)
            {
                var cell = _frontier.Dequeue();
                if (cell != entrance)
                    _cells.Add(cell);

                nav.Neighbours(cell, _neighbours);
                foreach (var next in _neighbours)
                {
                    if (nav.RoomOf(next) == room && _visited.Add(next))
                        _frontier.Enqueue(next);
                }
            }
        }

        private void Send(PartyProcession.Member member)
        {
            member.Sent = true;
            _host.WalkOut(member.Avatar);
        }

        private void SendAll(PartyProcession procession)
        {
            foreach (var member in procession.Members)
            {
                if (!member.Sent)
                    Send(member);
            }
        }

        private static void Enter(PartyProcession procession, ProcessionPhase phase, float timer)
        {
            procession.Phase = phase;
            procession.Timer = timer;
        }

        // NOTE: The in-progress quest this adventurer departs on (Depart sets the state before the members' status).
        private static bool TryFindDepartingQuest(int adventurerId, out Quest quest)
        {
            quest = null;
            if (!QuestManager.HasInstance)
                return false;

            foreach (var candidate in QuestManager.Instance.Quests)
            {
                if (candidate.State != QuestState.InProgress)
                    continue;
                foreach (var memberId in candidate.MemberIds)
                {
                    if (memberId != adventurerId)
                        continue;
                    quest = candidate;
                    return true;
                }
            }
            return false;
        }
    }
}