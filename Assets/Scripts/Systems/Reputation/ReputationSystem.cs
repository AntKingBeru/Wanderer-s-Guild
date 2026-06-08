using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReputationSystem : MonoBehaviour
{
    
    // Dictionary
    
    // To change bar fill percentage
    public Image BarFill; // Not sure if to listen to rider with this or not, rider says first letter lowercase, but its public but serializable
    public TextMeshProUGUI ReputationLevelText; // Not sure if to listen to rider with this or not, rider says first letter lowercase, but its public but serializable
    
    public int CurrentReputation => _currentReputation;
    public ReputationLevel CurrentState => _currentState;
    
    // Triggers only when reputation level changed
    public static event Action<ReputationLevel> ReputationLevelChanged;
    
    // Making this class singleton
    public static ReputationSystem Instance { get; private set; }


    private const int MaxReputation = 100;
    private const int MinReputation = -100;
    
    private int _currentReputation = 0;
    private ReputationLevel _currentState = ReputationLevel.Average;

    // Sorry if this is not good, we did not talk about the texts, we can use i18n later here
    private readonly Dictionary<ReputationLevel, string> _levelText = new Dictionary<ReputationLevel, string>()
    {
        { ReputationLevel.ExtremelyLow, "ExtremelyLow" },
        { ReputationLevel.Low, "Low" },
        { ReputationLevel.Average, "Average" },
        { ReputationLevel.High, "High" },
    };

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Initial check to set all UI components correctly
        CheckLevel();
        
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
        _currentReputation = newReputation;
        
        // Do more resets here if needed

        CheckLevel();
    }

    /**
     * Increasing or Decreasing reputation according to the given number
     */
    public void ChangeReputation(int reputation)
    {
        _currentReputation += reputation;
        
        if (_currentReputation > MaxReputation)
        {
            _currentReputation = MaxReputation;
        }
        
        if (_currentReputation < MinReputation)
        {
            _currentReputation = MinReputation;
        }
        

        CheckLevel();
    }
    
    private void CheckLevel()
    {
        // Checking if reputation level changed
        var newRep = _currentReputation switch
        {
            <  -50 => ReputationLevel.ExtremelyLow,
            <= -1  =>  ReputationLevel.Low,
            >   0   and <= 50 =>  ReputationLevel.Average,
            >   50  and <= 100 => ReputationLevel.High,
            _ => ReputationLevel.ExtremelyLow
        };
        
        if (newRep != _currentState)
        {
            _currentState = newRep;
            // TODO - change colors as needed
            ReputationLevelText.SetText(_levelText[_currentState]);
            ReputationLevelChanged?.Invoke(_currentState);
        };
        
        // TODO - Change color based on state
        BarFill.fillAmount = (_currentReputation + 100) / 200.0f;
    }
}
