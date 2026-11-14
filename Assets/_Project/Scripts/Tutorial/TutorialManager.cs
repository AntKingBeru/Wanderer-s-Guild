// The start tutorial's state (saved): pending (play / skip prompt), running or done — and what it holds back until it's done:
// saving, new requests, arrivals, and closing the panels it has locked.
using System;
using System.Collections.Generic;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". The stage is read in Awake (the slot is loaded at −50), so every
    // manager's Start already sees it.
    public class TutorialManager : Singleton<TutorialManager>, ISaveContributor
    {
        private readonly HashSet<MenuPanelId> _lockedPanels = new();
        private readonly HashSet<string> _completedGuides = new();
        private TutorialStage _stage;

        public TutorialStage Stage => _stage;
        public bool IsDone => _stage == TutorialStage.Done;

        // NOTE: Pending counts too — nothing happens behind the prompt.
        public bool BlocksNewRequests => !IsDone;
        public bool BlocksArrivals => !IsDone;

        protected override void OnSingletonAwake()
        {
            var data = SaveGameService.HasInstance ? SaveGameService.Instance.LoadedData : null;
            _stage = InitialStage(data);
            if (data != null)
            {
                foreach (var id in data.CompletedGuides)
                    _completedGuides.Add(id);
            }

            if (!IsDone)
                SaveBlockers.Block(this);
        }

        private void Start()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Register(this);
        }

        protected override void OnDestroy()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);
            SaveBlockers.Unblock(this);
            ScriptedQuests.Clear();
            base.OnDestroy();
        }

        // ---------- Panels ----------

        // False while the tutorial keeps this panel open (GamePanelHost asks before every close).
        public bool CanClose(MenuPanelId id) => !_lockedPanels.Contains(id);

        public void LockPanel(MenuPanelId id) => _lockedPanels.Add(id);
        public void UnlockPanel(MenuPanelId id) => _lockedPanels.Remove(id);

        // ---------- Room guides (28c) ----------

        public bool IsGuideCompleted(string guideId) => _completedGuides.Contains(guideId);

        public void MarkGuideCompleted(string guideId)
        {
            if (!string.IsNullOrEmpty(guideId))
                _completedGuides.Add(guideId);
        }

        // ---------- Flow ----------

        // The player chose the guided tutorial (28b runs it).
        public void BeginGuided()
        {
            if (_stage != TutorialStage.Pending)
                return;

            _stage = TutorialStage.Running;
            GameEventsRelay.OnTutorialStarted.Invoke();
        }

        // The player skipped: the tutorial's end state is built, then it's done.
        public void Skip()
        {
            if (IsDone)
                return;

            TutorialStartState.Apply();
            Finish(skipped: true);
        }

        // The guided tutorial reached its end (28b).
        public void Complete()
        {
            if (!IsDone)
                Finish(skipped: false);
        }

        public void WriteTo(GameSaveData data)
        {
            data.StartTutorialDone = IsDone;
            data.SetCompletedGuides(_completedGuides);
        }

        // NOTE: Unlocks everything, lets saving resume, posts the first real request batch, then saves — this save is the
        // guild's real starting point.
        private void Finish(bool skipped)
        {
            _stage = TutorialStage.Done;
            _lockedPanels.Clear();
            SaveBlockers.Unblock(this);

            if (RequestManager.HasInstance)
                RequestManager.Instance.GenerateBatchNow();

            GameEventsRelay.OnTutorialCompleted.Invoke(skipped);
            SaveAsync();
        }

        // NOTE: async void entry point; SaveAsync reports its own outcome (toast).
        private async void SaveAsync()
        {
            try
            {
                if (SaveGameService.HasInstance && SaveGameService.Instance.CanSave)
                    await SaveGameService.Instance.SaveAsync(SaveReason.Autosave);
            }
            catch (OperationCanceledException)
            {
                // Scene unloading.
            }
        }

        // NOTE: An active slot with nothing saved yet, or an unfinished tutorial → pending. No slot (Play in the Game scene) →
        // done, unless GameConfig › Tutorial › Tutorial Without Slot. Older saves report done (GameSaveData, version < 13).
        private static TutorialStage InitialStage(GameSaveData data)
        {
            if (!GameSession.HasActiveSlot)
                return GameConfig.Instance.Tutorial.TutorialWithoutSlot ? TutorialStage.Pending : TutorialStage.Done;
            if (data == null)
                return TutorialStage.Pending;
            return data.StartTutorialDone ? TutorialStage.Done : TutorialStage.Pending;
        }
    }
}