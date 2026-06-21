// Central configuration hub.
// Holds references to every system's ScriptableObject config asset.
// IMPORTANT: Set this script's execution order BEFORE GameEventRelay and all other managers
// (Edit > Project Settings > Script Execution Order)

using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    #region Adventurer System Configs
    [Header("Adventurer System")]
    [Tooltip("Global tunable numbers for adventurer creation, leveling, and ranking.")]
    [SerializeField] private AdventurerConfig adventurerConfig;

    [Tooltip("Pool of every ClassData asset that exists in the game (locked or unlocked).")]
    [SerializeField] private ClassDatabase classDatabase;

    [Tooltip("First/last name pools used by the random name generator.")]
    [SerializeField] private NameDatabase nameDatabase;
    #endregion
    
    #region Party System Config
    [Header("Party System")]
    [Tooltip("Party size limits, temporary-party trial window, and disband cooldown.")]
    [SerializeField] private PartyConfig partyConfig;
    #endregion
    
    #region Progression System Config
    [Header("Progression System")]
    [Tooltip("Guild rank XP thresholds.")]
    [SerializeField] private ProgressionConfig progressionConfig;
    #endregion
    
    public AdventurerConfig AdventurerConfig => adventurerConfig;
    public ClassDatabase ClassDatabase => classDatabase;
    public NameDatabase NameDatabase => nameDatabase;
    public PartyConfig PartyConfig => partyConfig;
    public ProgressionConfig ProgressionConfig => progressionConfig;
    
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Validate();
    }
    
    private void Validate()
    {
        if (!adventurerConfig)
            Debug.LogError("[GameManager] AdventurerConfig is not assigned.");
        if (!classDatabase)
            Debug.LogError("[GameManager] ClassDatabase is not assigned.");
        if (!nameDatabase)
            Debug.LogError("[GameManager] NameDatabase is not assigned.");
        if (!partyConfig)
            Debug.LogError("[GameManager] PartyConfig is not assigned.");
        if (!progressionConfig)
            Debug.LogError("[GameManager] ProgressionConfig is not assigned.");
    }
}