// Singleton reception line: assigns negative-Z queue slots and compacts the line as adventurers leave.

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-83)]
public class ReceptionQueue : MonoSingleton<ReceptionQueue>
{
    public readonly List<int> line = new();

    public int Join(int adventurerId)
    {
        if (!line.Contains(adventurerId))
            line.Add(adventurerId);
        return line.IndexOf(adventurerId);
    }
    
    public void Leave(int adventurerId)
        => line.Remove(adventurerId);
    
    public int IndexOf(int adventurerId)
        => line.IndexOf(adventurerId);

    public Vector3 SlotPosition(int queueIndex)
    {
        var head = WorldAnchors.Instance.ReceptionPoint;
        if (head == null)
            return Vector3.zero;
        var spacing = GameConfig.Instance.World.queueSpacing;
        return head.position - head.forward * (spacing * Mathf.Max(0, queueIndex));
    }
}