// Singleton registry of world path anchors (reception, board, exit) for adventurer navigation.

using UnityEngine;

[DefaultExecutionOrder(-84)]
public class WorldAnchors : MonoSingleton<WorldAnchors>
{
    [Header("Path Anchors")]
    [SerializeField] private Transform receptionPoint;
    [SerializeField] private Transform boardPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform spawnPoint;
    
    public Transform ReceptionPoint => receptionPoint;
    public Transform BoardPoint => boardPoint;
    public Transform ExitPoint => exitPoint;
    public Transform SpawnPoint => spawnPoint;
}