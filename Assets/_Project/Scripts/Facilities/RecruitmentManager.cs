// Game-scene owner of the applicant list: active while an office (ApplicantsEffect) is finished; accept, decline, expiry, save.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Applicants are rebuilt in Start (after AdventurerManager's Awake
    // has built its class/species lookups).
    public class RecruitmentManager : Singleton<RecruitmentManager>, ISaveContributor
    {
        private readonly List<Applicant> _applicants = new();
        private RecruitmentSettings _settings;
        private IReadOnlyList<SavedApplicant> _pendingRestore;

        public IReadOnlyList<Applicant> Applicants => _applicants;

        // Waiting slots from every finished room's ApplicantsEffect (0 = no active office).
        public int MaxWaiting
        {
            get
            {
                if (!FacilityManager.HasInstance)
                    return 0;

                var max = 0;
                foreach (var room in FacilityManager.Instance.Rooms)
                {
                    var effect = room.Data.Effect<ApplicantsEffect>();
                    if (effect)
                        max = Mathf.Max(max, effect.MaxWaitingAt(room.Level));
                }
                return max;
            }
        }

        // True while a Guild Master's Office is finished — arrivals then become applicants.
        public bool AcceptsApplicants => MaxWaiting > 0;

        public bool HasRoom => _applicants.Count < MaxWaiting;

        private static long Now => RequestManager.NowMinutes;

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Recruitment;
            var data = SaveGameService.HasInstance ? SaveGameService.Instance.LoadedData : null;
            if (data != null)
                _pendingRestore = data.Applicants;
        }

        private void OnEnable()
        {
            GameEventsRelay.OnTimeTick.AddListener(OnTimeTick);
            GameEventsRelay.OnFacilityRemoved.AddListener(OnFacilityRemoved);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnTimeTick.RemoveListener(OnTimeTick);
            GameEventsRelay.OnFacilityRemoved.RemoveListener(OnFacilityRemoved);
        }

        private void Start()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Register(this);

            Restore();
        }

        protected override void OnDestroy()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);
            base.OnDestroy();
        }

        public Applicant Find(int id)
        {
            return _applicants.FirstOrDefault(applicant => applicant.Id == id);
        }

        // Names in use by applicants (the roster's names are checked separately).
        public IEnumerable<string> Names
        {
            get { return _applicants.Select(applicant => applicant.Adventurer.Name); }
        }

        // A new arrival starts waiting. False when recruitment isn't active or the list is full.
        public bool AddApplicant(Adventurer adventurer)
        {
            if (adventurer == null || !HasRoom)
                return false;

            Applicant applicant = new(adventurer, Now + _settings.WaitMinutes);
            _applicants.Add(applicant);
            GameEventsRelay.OnApplicantArrived.Invoke(applicant);
            return true;
        }

        // Moves the applicant into the roster. False if they're gone or the guild is full (they keep waiting).
        public bool Accept(int applicantId)
        {
            var applicant = Find(applicantId);
            if (applicant == null || !AdventurerManager.HasInstance)
                return false;
            if (!AdventurerManager.Instance.TryAddMember(applicant.Adventurer))
                return false;

            End(applicant, ApplicantEndReason.Accepted);
            return true;
        }

        public bool Decline(int applicantId)
        {
            var applicant = Find(applicantId);
            if (applicant == null)
                return false;

            End(applicant, ApplicantEndReason.Declined);
            return true;
        }

        // Debug and testing: every waiting applicant leaves now (as if their time ran out).
        public void ExpireAllNow()
        {
            foreach (var applicant in new List<Applicant>(_applicants))
                End(applicant, ApplicantEndReason.Expired);
        }

        public void WriteTo(GameSaveData data)
        {
            var saved = _applicants.Select(applicant => applicant.ToSaved()).ToList();
            data.SetApplicants(saved);
        }

        // NOTE: Due applicants are collected first, then removed — never modifies the list while iterating it.
        private void OnTimeTick(GameDateTime now)
        {
            List<Applicant> due = null;
            foreach (var applicant in _applicants.Where(applicant => now.TotalMinutes >= applicant.ExpiresAtMinutes))
            {
                (due ??= new List<Applicant>()).Add(applicant);
            }

            if (due == null) return;
            foreach (var applicant in due)
                End(applicant, ApplicantEndReason.Expired);
        }

        // NOTE: Only a removal can switch recruitment off (an upgrade keeps the current level working).
        private void OnFacilityRemoved(PlacedFacility _)
        {
            if (AcceptsApplicants || _applicants.Count == 0)
                return;

            foreach (var applicant in new List<Applicant>(_applicants))
                End(applicant, ApplicantEndReason.OfficeClosed);
        }

        private void End(Applicant applicant, ApplicantEndReason reason)
        {
            _applicants.Remove(applicant);
            GameEventsRelay.OnApplicantLeft.Invoke(applicant, reason);
        }

        // NOTE: Without an active office the saved list can't be reached — dropped silently. Unknown class/species are
        // dropped with a warning (by AdventurerManager.TryRebuild).
        private void Restore()
        {
            if (_pendingRestore == null)
                return;

            if (AcceptsApplicants && AdventurerManager.HasInstance)
            {
                foreach (var saved in _pendingRestore)
                {
                    if (AdventurerManager.Instance.TryRebuild(saved.Adventurer, out Adventurer adventurer))
                        _applicants.Add(new Applicant(adventurer, saved.ExpiresAtMinutes));
                }
            }

            _pendingRestore = null;
        }
    }
}