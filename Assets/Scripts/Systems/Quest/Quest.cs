// Runtime guild quest: source-request facts + player configuration + guarded lifecycle state.

public class Quest
{
    public int Id { get; }
    public int SourceRequestId { get; }
    public QuestCategory Category { get; }
    public string Objective { get; }
    public QuestDifficulty Difficulty { get; }
    public int RewardGold { get; }
    public GameDate ExpirationDate { get; }
    public QuestConfiguration Config { get; }
    public QuestState State { get; private set; } = QuestState.Draft;
    
    public Quest(int id, Request source, QuestConfiguration config, GameDate expirationDate)
    {
        Id = id;
        SourceRequestId = source.Id;
        Category = source.Category;
        Objective = source.Objective;
        Difficulty = source.Difficulty;
        RewardGold = source.RewardGold;
        Config = config;
        ExpirationDate = expirationDate;
    }
    
    public int GuildCut => RewardGold * Config.rewardSplit.guildPercent / 100;
    
    public bool TrySetState(QuestState next)
    {
        if (!CanTransition(State, next))
            return false;
        State = next;
        return true;
    }
    
    public bool IsExpired(GameDate now)
        => now.CompareTo(ExpirationDate) >= 0;
    
    private static bool CanTransition(QuestState from, QuestState to) => (from, to) switch
    {
        (QuestState.Draft, QuestState.Posted) => true,
        (QuestState.Posted, QuestState.InProgress) => true,
        (QuestState.Posted, QuestState.Expired) => true,
        (QuestState.InProgress, QuestState.Succeeded) => true,
        (QuestState.InProgress, QuestState.Failed) => true,
        _ => false
    };
}