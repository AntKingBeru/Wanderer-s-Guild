// Runtime state for one placed room in the guild.
// Created by BuildManager when the player confirms construction.
// Tracks the build progress and applies/removes stat boosts from AdventurerManager via BuildManager.
// Uses the State pattern: Idle → UnderConstruction → Built.

using System;
using UnityEngine;

[Serializable]
public class RoomInstance
{
    // Unique runtime ID for this placed room (GUID).
    private readonly string _instanceId;
    // Which template this room was built from.
    private readonly RoomDefinition _definition;
    // Absolute in-game hour at which construction finishes.
    private float _completionHour;
    // Current lifecycle state.
    private RoomState _state;

    // Progress from 0 to 1. Exposed to the progress bar UI.
    private float _progress;

    public string InstanceId => _instanceId;
    public RoomDefinition Definition => _definition;
    public RoomState State => _state;
    // 0 to 1 fraction. Updated every hour tick by BuildManager.
    public float Progress => _progress;
    public float CompletionHour => _completionHour;
    public bool IsBuilt => _state == RoomState.Built;
    
    public RoomInstance(RoomDefinition definition, float startHour)
    {
        _instanceId = Guid.NewGuid().ToString();
        _definition = definition;
        _state = RoomState.UnderConstruction;
        _completionHour = startHour + definition.BuildTimeHours;
        _progress = 0f;
    }
    
    // Called by BuildManager each hour. Returns true the frame construction finishes.
    public bool Tick(float currentHour, float startHour)
    {
        if (_state == RoomState.Built)
            return false;

        var totalDuration = _definition.BuildTimeHours;
        // Clamp to 1 to avoid overflow on the exact completion hour.
        _progress = Mathf.Clamp01((currentHour - startHour) / totalDuration);

        if (currentHour >= _completionHour)
        {
            _progress = 1f;
            _state = RoomState.Built;
            return true;
        }

        return false;
    }
}