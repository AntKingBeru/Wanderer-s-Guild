// Singleton data manager for guild reputation.
// Range: -100 (worst) to +100 (best). Starts at 0 (Average).
// Fires two events:
//   OnReputationChanged — every value change (int newValue)
//   OnReputationLevelChanged — only when the ReputationLevel tier flips
// UI is handled entirely by ReputationHUDController.
// Integration: call ChangeReputation() from QuestManager on quest completion/failure, and from any other system that should affect guild standing.

using System;
using UnityEngine;

public class ReputationSystem : MonoBehaviour
{
    public static ReputationSystem Instance { get; private set; }
    
    // Fires on every reputation value change, including resets.
    // Subscribe in ReputationHUDController to keep the bar fill current.
    public static event Action<int> OnReputationChanged;
    
    // Fires only when the tier (ExtremelyLow / Low / Average / High) changes.
    // Subscribe wherever reputation tier matters (adventurer morale, factory rate, etc.)
    public static event Action<ReputationLevel> OnReputationLevelChanged;
    
    public const int MaxReputation = 100;
    public const int MinReputation = -100;
    
    private int _currentReputation;
    private ReputationLevel _currentLevel = ReputationLevel.Average;
    
    public int CurrentReputation => _currentReputation;
    public ReputationLevel CurrentLevel => _currentLevel;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

       BroadcastState();
    }
    
    // Resets reputation to given number, default is 0
    public void ResetReputation(int reputation = 0)
    {
        _currentReputation = Mathf.Clamp(reputation, MinReputation, MaxReputation);
        BroadcastState();
    }
    
    // Increasing or Decreasing reputation according to the given number
    public void ChangeReputation(int delta)
    {
        _currentReputation = Mathf.Clamp(_currentReputation + delta, MinReputation, MaxReputation);
        BroadcastState();
    }
    
    private void BroadcastState()
    {
        OnReputationChanged?.Invoke(_currentReputation);
        var newLevel = ComputeLevel(_currentReputation);
        if (newLevel == _currentLevel)
            return;
        _currentLevel = newLevel;
        OnReputationLevelChanged?.Invoke(_currentLevel);
    }
    
    // Pure function - no side effects. Determines tier from a raw reputation value.
    // Boundaries:
    //   ExtremelyLow : -100 to -51
    //   Low : -50 to -1
    //   Average : 0 to 50
    //   High : 51 to 100
    private static ReputationLevel ComputeLevel(int reputation) => reputation switch
    {
        < -50 => ReputationLevel.ExtremelyLow,
        >= -50 and <= -1 => ReputationLevel.Low,
        >= 0 and <= 50 => ReputationLevel.Average,
        _ => ReputationLevel.High
    };
}
