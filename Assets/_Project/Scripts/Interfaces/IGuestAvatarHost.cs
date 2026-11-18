// What guest avatars (applicants) need from the avatar owner beyond a procession's needs: a new avatar, and forgetting its
// furniture exit.
using UnityEngine;

namespace WanderersGuild
{
    public interface IGuestAvatarHost : IProcessionHost
    {
        // A new avatar for this adventurer standing on the cell (not added to any list).
        AdventurerAvatar CreateAvatar(Adventurer adventurer, Vector2Int cell);

        // Drops the remembered "get up here" cell beside furniture (on despawn).
        void ForgetExit(int adventurerId);
    }
}