// Database of all authored ClassData assets.
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(fileName = "ClassDatabase", menuName = "Wanderer's Guild/Databases/Class", order = 1)]
    public class ClassDatabase : ScriptableObjectDatabase<ClassData> { }
}