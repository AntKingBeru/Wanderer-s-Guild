// Queues a toast for each result — quests (party ready, succeeded with the guild's gold, failed, expired) and promotion exams.
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Uses its own SaveToastView (a generic toast) on its own document, so results never cut off a save toast.
    public class ResultToastPresenter : MonoBehaviour
    {
        [SerializeField] private SaveToastView view;

        private readonly Queue<(string message, bool isError)> _queue = new();
        private Tween _wait;
        private bool _isShowing;

        private void OnEnable()
        {
            GameEventsRelay.OnQuestPartyReady.AddListener(OnPartyReady);
            GameEventsRelay.OnQuestEnded.AddListener(OnQuestEnded);
            GameEventsRelay.OnPromotionExamEnded.AddListener(OnExamEnded);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnQuestPartyReady.RemoveListener(OnPartyReady);
            GameEventsRelay.OnQuestEnded.RemoveListener(OnQuestEnded);
            GameEventsRelay.OnPromotionExamEnded.RemoveListener(OnExamEnded);
            if (_wait.IsActive())
                _wait.Kill();
            _queue.Clear();
            _isShowing = false;
        }

        private void OnPartyReady(Quest quest) => Enqueue($"{quest.Request.DisplayName}: party ready", false);

        // NOTE: Cancelling is the player's own action — no toast.
        private void OnQuestEnded(Quest quest, QuestEndReason reason)
        {
            var title = quest.Request.DisplayName;
            switch (reason)
            {
                case QuestEndReason.Succeeded:
                    long guildGold = QuestFormatter.GuildShare(quest, quest.MemberIds.Count);
                    Enqueue($"Quest succeeded: {title}  +{guildGold:N0}g", false);
                    break;
                case QuestEndReason.Failed:
                    Enqueue($"Quest failed: {title}", true);
                    break;
                case QuestEndReason.Expired:
                    Enqueue($"Quest expired: {title}", true);
                    break;
            }
        }

        private void OnExamEnded(Adventurer adventurer, GuildRank target, bool passed) =>
            Enqueue(passed ? $"{adventurer.Name} passed the Rank {target} exam"
                           : $"{adventurer.Name} failed the Rank {target} exam", !passed);

        private void Enqueue(string message, bool isError)
        {
            if (!view)
                return;

            _queue.Enqueue((message, isError));
            if (!_isShowing)
                ShowNext();
        }

        // NOTE: The next toast waits for fade in + hold + fade out, so toasts never overlap or cut each other off.
        private void ShowNext()
        {
            if (_queue.Count == 0)
            {
                _isShowing = false;
                return;
            }

            _isShowing = true;
            var (message, isError) = _queue.Dequeue();

            var hold = GameConfig.Instance.QuestPanel.ResultToastHold;
            var fade = GameConfig.Instance.SaveGame.ToastFadeDuration;
            view.Show(message, isError, hold);

            _wait = DOVirtual.DelayedCall(fade * 2f + hold, ShowNext).SetUpdate(true).SetLink(gameObject);
        }
    }
}