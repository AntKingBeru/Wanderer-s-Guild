// One guild adventurer: identity, species and class, experience (level derived), rank + progress, wealth, status, exam, gear,
// supper buff, injury, party, return from a recall.
namespace WanderersGuild
{
    public sealed class Adventurer
    {
        private readonly ExperienceTable _levels;

        public Adventurer(int id, string name, SpeciesData species, ClassData classData, long experience, GuildRank rank,
                          int wealth, AdventurerStatus status, long joinedAtMinutes, StatBlock retainedGrowth, ExperienceTable levels,
                          float rankProgress = 0f, long examReturnsAtMinutes = 0, EquipmentData[] equipment = null,
                          float wellFedBonus = 0f, InjurySeverity injury = InjurySeverity.None, long injuredUntilMinutes = 0,
                          long returningUntilMinutes = 0)
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
            WellFedBonus = System.Math.Max(0f, wellFedBonus);
            Injury = injury;
            InjuredUntilMinutes = System.Math.Max(0L, injuredUntilMinutes);
            ReturningUntilMinutes = System.Math.Max(0L, returningUntilMinutes);

            // NOTE: Each item goes into its own slot; nulls (unknown ids) leave the slot empty.
            if (equipment == null)
                return;
            foreach (var item in equipment)
            {
                if (item)
                    Equipment.Set(item.Slot, item);
            }
        }

        // Unique per guild (saved).
        public int Id { get; }
        public string Name { get; }
        public SpeciesData Species { get; }
        public ClassData Class { get; }

        // Total XP in the current class (cumulative table); clamped at the level cap.
        public long Experience { get; private set; }

        // Adventurer rank — raised only by passing a promotion exam.
        public GuildRank Rank { get; private set; }

        // Progress toward the next rank's exam (from successful quests; capped by the promotion system).
        public float RankProgress { get; private set; }

        public int Wealth { get; private set; }

        // Idle, signed up, on a quest, in an exam, injured or returning. Changed by the quest, promotion and injury systems.
        public AdventurerStatus Status { get; private set; }
        public long JoinedAtMinutes { get; }

        // When a running promotion exam ends; 0 = none.
        public long ExamReturnsAtMinutes { get; private set; }

        // Stat growth kept through class advancement (0 until advancement exists).
        public StatBlock RetainedGrowth { get; }

        // Worn gear (changed by the armory).
        public AdventurerEquipment Equipment { get; } = new();

        // Quest power bonus from supper (0 = not well-fed). Set only by the tavern at supper.
        public float WellFedBonus { get; private set; }

        public bool IsWellFed => WellFedBonus > 0f;

        // Current injury (None when healthy) and when it heals.
        public InjurySeverity Injury { get; private set; }
        public long InjuredUntilMinutes { get; private set; }

        // When a recalled adventurer gets back (meaningful only while Returning).
        public long ReturningUntilMinutes { get; private set; }

        // The party they belong to (0 = none) and its teamwork power bonus. Set only by PartyManager (not saved
        // here — rebuilt from the saved parties on load).
        public int PartyId { get; private set; }
        public float TeamworkBonus { get; private set; }
        public bool IsInParty => PartyId != 0;

        public int Level => _levels.LevelFor(Experience);
        public bool IsMaxLevel => Level >= _levels.MaxLevel;
        public float LevelProgress => _levels.Progress(Experience);

        // The stat formula without gear (class, species, level, retained growth).
        public StatBlock BaseStats => AdventurerStats.Compute(Class, Species, Level, RetainedGrowth);

        // Stats including gear — what quests, exams and the roster use.
        public StatBlock Stats => BaseStats + Equipment.Bonus;

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

        // Spends personal gold. False (nothing spent) if they can't pay.
        public bool TrySpendWealth(int amount)
        {
            if (amount < 0 || amount > Wealth)
                return false;
            Wealth -= amount;
            return true;
        }

        // Sets the supper buff (0 clears it). Only the tavern calls this.
        public void SetWellFed(float bonus) => WellFedBonus = System.Math.Max(0f, bonus);

        public void SetParty(int partyId, float teamworkBonus)
        {
            PartyId = partyId;
            TeamworkBonus = System.Math.Max(0f, teamworkBonus);
        }

        public void ClearParty() => SetParty(0, 0f);

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

        // NOTE: Recall lifecycle — only the quest system calls these.
        public void StartReturn(long untilMinutes)
        {
            ReturningUntilMinutes = System.Math.Max(0L, untilMinutes);
            Status = AdventurerStatus.Returning;
        }

        // Home again: Idle.
        public void FinishReturn()
        {
            ReturningUntilMinutes = 0;
            Status = AdventurerStatus.Idle;
        }

        // NOTE: Injury lifecycle — only the injury system calls these. A new injury replaces a lighter or shorter one only
        // if it's worse or lasts longer (never shortens a recovery).
        public void Injure(InjurySeverity severity, long untilMinutes)
        {
            if (severity == InjurySeverity.None)
                return;

            if (severity > Injury)
                Injury = severity;
            InjuredUntilMinutes = System.Math.Max(InjuredUntilMinutes, untilMinutes);
            Status = AdventurerStatus.Injured;
        }

        // Healed: back to Idle.
        public void Recover()
        {
            ClearInjury();
            Status = AdventurerStatus.Idle;
        }

        // Clears the injury without touching the status (load repair).
        public void ClearInjury()
        {
            Injury = InjurySeverity.None;
            InjuredUntilMinutes = 0;
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
                RankProgress, ExamReturnsAtMinutes, Equipment.ToIds(), WellFedBonus, Injury, InjuredUntilMinutes,
                ReturningUntilMinutes);

        private void EndExam()
        {
            ExamReturnsAtMinutes = 0;
            Status = AdventurerStatus.Idle;
        }
    }
}