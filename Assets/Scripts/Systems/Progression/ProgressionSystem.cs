// Singleton data manager for guild progression.
// range: There are 7 levels, we start at level 1
// Fires two events:
//   OnProgressionXpChanged — every xp change
//   OnProgressionRankChanged — only when rank level changed
// UI is handled entirely by ProgressionHUDController.
// Integration: Please fill me

using System.Collections.Generic;
using UnityEngine;

public class ProgressionSystem : MonoBehaviour
{
    private const int MaxProgressionRank = 7;
    
    // Making this class singleton
    public static ProgressionSystem Instance { get; private set; }

    // Current rank from 0 to 7
    private int _currentRank;
    private int _currentXp;
    
    public int CurrentRank => _currentRank;
    public int CurrentXp => _currentXp;
    public int CurrentThreshold => _rankThreshold[_currentRank + 1];
    
    private readonly Dictionary<int, int> _rankThreshold = new()
    {
        {1, 100},
        {2, 200},
        {3, 300},
        {4, 400},
        {5, 500},
        {6, 600},
        {7, 700},
    };
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        RankUp();
    }
    
    public void GainXp(int xp)
    {
        if (IsHighestRank())
        {
            return;
        }
        _currentXp += xp;
        RankUp();
    }

    /**
     * Checks recursively for rank up based on the amount of xp obtained
     */
    private void RankUp()
    {
        if (IsHighestRank())
        {
            return;
        }
            
        var currentThreshold = _rankThreshold[_currentRank + 1];
        var lastRank = _currentRank; // Saving to check if we need to trigger event
    
        // Adding xp till we highest level of we used all the xp
        while (_currentXp >= currentThreshold && !IsHighestRank())
        {
            _currentXp -= currentThreshold;
            _currentRank += 1;
            if (IsHighestRank())
            { 
                break;
            }
            
            currentThreshold = _rankThreshold[_currentRank + 1];
        }
        
        // No need to continue
        if (lastRank != _currentRank)
        {
            GameEventRelay.Instance.onProgressionRankChanged.Invoke(_currentRank);
        }
        
        GameEventRelay.Instance.onProgressionXpChanged.Invoke(_currentXp, currentThreshold);
    }

    // Checks If I am highest rank to avoid excess code
    private bool IsHighestRank()
    {
        return _currentRank >= MaxProgressionRank;
    }
}