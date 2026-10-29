// One guild adventurer: identity, species and class, experience (level derived), rank + rank progress, wealth, status, exam.
namespace WanderersGuild
{
    public sealed class Adventurer
    {
        private readonly ExperienceTable _levels;

        public Adventurer(int id, string name, SpeciesData species, ClassData classData, long experience, GuildRank rank,
                          int wealth, AdventurerStatus status, long joinedAtMinutes, StatBlock retainedGrowth, ExperienceTable levels,
                          float rankProgress = 0f, long examReturnsAtMinutes = 0)
        {
            _levels = levels;
            Id = id;
            Name = name;
            Species = species;
            Class = classData;
            Experience = System.Math.Clamp(experience, 0L, levels.ThresholdFor(levels.MaxLevel));
            Rank = rank;
            Wealth = System.Math.Max(0, wealth);
            Status = status;
            JoinedAtMinutes = joinedAtMinutes;
            RetainedGrowth = retainedGrowth;
            RankProgress = System.Math.Max(0f, rankProgress);
            ExamReturnsAtMinutes = System.Math.Max(0L, examReturnsAtMinutes);
        }

        // Unique per guild (saved).
        public int Id { get; }
        public string Name { get; }
        public SpeciesData Species { get; }
        public ClassData Class { get; }

        // Total XP in the current class (D&D-style cumulative table); clamped at the level cap.
        public long Experience { get; private set; }

        // Adventurer rank — raised only by passing a promotion exam.
        public GuildRank Rank { get; private set; }

        // Progress toward the next rank's exam (from successful quests; capped by the promotion system).
        public float RankProgress { get; private set; }

        public int Wealth { get; private set; }

        // Idle, signed up, on a quest, or in an exam. Changed by the quest and promotion systems.
        public AdventurerStatus Status { get; private set; }
        public long JoinedAtMinutes { get; }

        // When a running promotion exam ends; 0 = none.
        public long ExamReturnsAtMinutes { get; private set; }

        // Stat growth kept through class advancement (0 until advancement exists).
        public StatBlock RetainedGrowth { get; }

        public int Level => _levels.LevelFor(Experience);
        public bool IsMaxLevel => Level >= _levels.MaxLevel;
        public float LevelProgress => _levels.Progress(Experience);
        public StatBlock Stats => AdventurerStats.Compute(Class, Species, Level, RetainedGrowth);

        // Adds XP (clamped at the cap). Returns how many levels were gained.
        public int AddExperience(long amount)
        {
            if (amount <= 0 || IsMaxLevel)
                return 0;

            var before = Level;
            var cap = _levels.ThresholdFor(_levels.MaxLevel);
            Experience = amount > cap - Experience ? cap : Experience + amount;
            return Level - before;
        }

        // Adds personal gold (saturating; never below 0).
        public void AddWealth(int amount)
        {
            if (amount <= 0)
                return;
            Wealth = amount > int.MaxValue - Wealth ? int.MaxValue : Wealth + amount;
        }

        // True if this call changed it.
        public bool SetStatus(AdventurerStatus status)
        {
            if (Status == status)
                return false;
            Status = status;
            return true;
        }

        // Adds rank progress, never beyond the cap. True if it changed.
        public bool AddRankProgress(float amount, float cap)
        {
            var next = System.Math.Min(cap, RankProgress + System.Math.Max(0f, amount));
            if (next <= RankProgress)
                return false;

            RankProgress = next;
            return true;
        }

        // NOTE: Exam lifecycle — only the promotion system calls these.
        public void StartExam(long returnsAtMinutes)
        {
            ExamReturnsAtMinutes = returnsAtMinutes;
            Status = AdventurerStatus.InExam;
        }

        // Passed: the new rank, progress back to 0.
        public void PassExam(GuildRank newRank)
        {
            Rank = newRank;
            RankProgress = 0f;
            EndExam();
        }

        // Failed: progress halves.
        public void FailExam()
        {
            RankProgress *= 0.5f;
            EndExam();
        }

        // Clears an exam without a result (repairs inconsistent saves).
        public void ClearExam(bool resetStatus)
        {
            ExamReturnsAtMinutes = 0;
            if (resetStatus)
                Status = AdventurerStatus.Idle;
        }

        public SavedAdventurer ToSaved() =>
            new(Id, Name, Species.Id, Class.Id, Experience, Rank, Wealth, Status, JoinedAtMinutes, RetainedGrowth,
                RankProgress, ExamReturnsAtMinutes);

        private void EndExam()
        {
            ExamReturnsAtMinutes = 0;
            Status = AdventurerStatus.Idle;
        }
    }
}