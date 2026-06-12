// Singleton data manager for guild reputation. Range: -100 to +100, starts at 0.
// Raises events through GameEventRelay instead of static C# events.

using UnityEngine;

public class ReputationSystem : MonoBehaviour
{
    public static ReputationSystem Instance { get; private set; }

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
    
    // Resets reputation to the given value (default 0).
    public void ResetReputation(int reputation = 0)
    {
        _currentReputation = Mathf.Clamp(reputation, MinReputation, MaxReputation);
        BroadcastState();
    }

    // Increases or decreases reputation by delta; clamps to valid range.
    public void ChangeReputation(int delta)
    {
        _currentReputation = Mathf.Clamp(_currentReputation + delta, MinReputation, MaxReputation);
        BroadcastState();
    }

    private void BroadcastState()
    {
        if (!GameEventRelay.Instance)
            return;

        GameEventRelay.Instance.OnReputationChanged.Invoke(_currentReputation);

        var newLevel = ComputeLevel(_currentReputation);
        if (newLevel == _currentLevel)
            return;

        _currentLevel = newLevel;
        GameEventRelay.Instance.OnReputationLevelChanged.Invoke(_currentLevel);
    }

    // Pure function — maps a raw reputation value to its ReputationLevel tier.
    private static ReputationLevel ComputeLevel(int rep)
    {
        return rep switch
        {
            <= (int)ReputationLevel.ExtremelyLow => ReputationLevel.ExtremelyLow,
            <= (int)ReputationLevel.Low => ReputationLevel.Low,
            <= (int)ReputationLevel.Average => ReputationLevel.Average,
            _ => ReputationLevel.High
        };
    }
}
