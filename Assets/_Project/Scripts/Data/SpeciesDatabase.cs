// Database of all authored SpeciesData assets.
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(fileName = "SpeciesDatabase", menuName = "Wanderer's Guild/Databases/Species", order = 0)]
    public class SpeciesDatabase : ScriptableObjectDatabase<SpeciesData> { }
}