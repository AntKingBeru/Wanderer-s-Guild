// What a party procession needs from the avatar owner: the entrance, walking an avatar somewhere, and the normal walk-out.
using System;
using UnityEngine;

namespace WanderersGuild
{
    public interface IProcessionHost
    {
        GuildNavGrid Nav { get; }

        // The entrance cell (false if the guild has none).
        bool TryGetEntrance(out Vector2Int cell);

        // Walks an avatar to a cell; onArrived on arrival. False if there's no path (the avatar keeps what it was doing).
        bool TryWalkTo(AdventurerAvatar avatar, Vector2Int cell, Action onArrived);

        // The normal departure: walk to the entrance and despawn there.
        void WalkOut(AdventurerAvatar avatar);

        // A cell's center at floor height.
        Vector3 CellCentre(Vector2Int cell);
    }
}