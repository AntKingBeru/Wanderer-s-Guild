using System;
using UnityEngine;

public class ReputationSystem : MonoBehaviour
{
    
    public int currentReputation = 0;
    public ReputationLevel currentState = ReputationLevel.Average;
    
    // Triggers with any change to reputation
    public static event Action<int> ReputationChanged;
    
    // Triggers only when reputation level changed
    public static event Action<ReputationLevel> ReputationLevelChanged;
    
    // Making this class singleton
    private static ReputationSystem Instance { get; set; }


    private const int MaxReputation = 100;
    
    private const int MinReputation = -100;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Cross-Scene singleton
        DontDestroyOnLoad(gameObject);
    }

    /**
     * Resets reputation to given number, default is 0
     */
    public void ResetReputation(int reputation = 0)
    {
        // Better be safe than sorry
        var newReputation = reputation;
        if (newReputation is < MinReputation or > MaxReputation)
        {
            newReputation = 0;
        } 
        currentReputation = newReputation;
        
        // Do more resets here if needed

        NotifyReputationChanged();
    }

    /**
     * Increasing or Decreasing reputation according to the given number
     */
    public void ChangeReputation(int reputation)
    {
        if (currentReputation + reputation > MaxReputation) currentReputation = MaxReputation;
        
        if (currentReputation + reputation < MinReputation) currentReputation = MinReputation;
        
        currentReputation += reputation;

        NotifyReputationChanged();
    }
    
    private void NotifyReputationChanged()
    {
        // ?. checks if someone is subscribed before firing
        ReputationChanged?.Invoke(currentReputation);
        
        // Checking if reputation level changed
        var newRep = currentReputation switch
        {
            >= -100 and <= -50 => ReputationLevel.ExtremelyLow,
            >  -50  and <= -1 => ReputationLevel.Low,
            >   0   and <= 50 => ReputationLevel.Average,
            >   50  and <= 100 => ReputationLevel.High,
            _ => ReputationLevel.ExtremelyLow
        };

        if (newRep == currentState) return;
        
        currentState = newRep;
        ReputationLevelChanged?.Invoke(newRep);
    }
}
