using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestSystem : MonoBehaviour
{

    
    // To change bar fill percentage
    public Image XpBarFill; // Not sure if to listen to rider with this or not, rider says first letter lowercase, but its public but serializable
    public TextMeshProUGUI RankText; // Not sure if to listen to rider with this or not, rider says first letter lowercase, but its public but serializable
    public TextMeshProUGUI XpBarText; // Displays currentXp / threshold
    
    public int CurrentRank => _currentRank;
    
    // Triggers on rank level change
    public static event Action<int> RankLevelChanged;
    
    // Making this class singleton
    public static QuestSystem Instance { get; private set; }


    // Current rank from 0 to 7
    private int _currentRank = 0;
    
    private readonly Dictionary<int, string> _rankText = new Dictionary<int, string>()
    {
        {0, "0"},
        {1, "1"},
        {2, "2"},
        {3, "3"},
        {4, "4"},
        {5, "5"},
        {6, "6"},
        {7, "7"},
    };
    
    private readonly Dictionary<int, int> _rankThreshold = new Dictionary<int, int>()
    {
        {1, 100},
        {2, 200},
        {3, 300},
        {4, 400},
        {5, 500},
        {6, 600},
        {7, 700},
    };

    private int _currentXp = 0;
    
    // For easier access
    //private int _currentThreshold = 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Setting all UI
        RankText.SetText(_rankText[_currentRank]);
        RankUp();
        
        // Cross-Scene singleton
        DontDestroyOnLoad(gameObject);
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
     * Checks recursively for rank up based on amount of xp obtained
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
            // TODO - change colors as needed
            RankText.SetText(_rankText[_currentRank]);
            RankLevelChanged?.Invoke(_currentRank);
        }
        
        // Update UI here
        XpBarFill.fillAmount = (float)_currentXp / (float)currentThreshold;
        XpBarText.SetText(_currentXp + "/" + currentThreshold);
        
    }

    // Checks If I am highest rank to avoid excess code
    private bool IsHighestRank()
    {
        return _currentRank >= 7;
    }
}
