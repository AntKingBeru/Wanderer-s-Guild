// Singleton reception line: assigns negative-Z queue slots and compacts the line as adventurers leave.

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-83)]
public class ReceptionQueue : MonoSingleton<ReceptionQueue>
{
    public readonly List<int> Line = new();

    public int Join(int adventurerId)
    {
        if (!Line.Contains(adventurerId))
            Line.Add(adventurerId);
        return Line.IndexOf(adventurerId);
    }
    
    public void Leave(int adventurerId)
        => Line.Remove(adventurerId);
    
    public int IndexOf(int adventurerId)
        => Line.IndexOf(adventurerId);

    public Vector3 SlotPosition(int queueIndex)
    {
        var head = WorldAnchors.Instance.ReceptionPoint;
        if (head == null)
            return Vector3.zero;
        var spacing = GameConfig.Instance.World.queueSpacing;
        return head.position - head.forward * (spacing * Mathf.Max(0, queueIndex));
    }
}