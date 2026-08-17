// Database of all authored RequestData assets.
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(fileName = "RequestDatabase", menuName = "Wanderer's Guild/Databases/Request", order = 2)]
    public class RequestDatabase : ScriptableObjectDatabase<RequestData> { }
}