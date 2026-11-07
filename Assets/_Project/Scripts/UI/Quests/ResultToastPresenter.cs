// Queues a toast for each result — quests, promotion exams, and applicants arriving / giving up.
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
        private int _officeClosedFrame = -1;

        private void OnEnable()
        {
            GameEventsRelay.OnQuestPartyReady.AddListener(OnPartyReady);
            GameEventsRelay.OnQuestEnded.AddListener(OnQuestEnded);
            GameEventsRelay.OnPromotionExamEnded.AddListener(OnExamEnded);
            GameEventsRelay.OnApplicantArrived.AddListener(OnApplicantArrived);
            GameEventsRelay.OnApplicantLeft.AddListener(OnApplicantLeft);
            GameEventsRelay.OnArmorySalesCompleted.AddListener(OnArmorySales);
            GameEventsRelay.OnArmoryRestocked.AddListener(OnArmoryRestocked);
            GameEventsRelay.OnSupperServed.AddListener(OnSupperServed);
            GameEventsRelay.OnPotionsRefused.AddListener(OnPotionsRefused);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnQuestPartyReady.RemoveListener(OnPartyReady);
            GameEventsRelay.OnQuestEnded.RemoveListener(OnQuestEnded);
            GameEventsRelay.OnPromotionExamEnded.RemoveListener(OnExamEnded);
            GameEventsRelay.OnApplicantArrived.RemoveListener(OnApplicantArrived);
            GameEventsRelay.OnApplicantLeft.RemoveListener(OnApplicantLeft);
            GameEventsRelay.OnArmorySalesCompleted.RemoveListener(OnArmorySales);
            GameEventsRelay.OnArmoryRestocked.RemoveListener(OnArmoryRestocked);
            GameEventsRelay.OnSupperServed.RemoveListener(OnSupperServed);
            GameEventsRelay.OnPotionsRefused.RemoveListener(OnPotionsRefused);
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
                    var guildGold = QuestFormatter.GuildShare(quest, quest.MemberIds.Count);
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

        private void OnApplicantArrived(Applicant applicant)
        {
            var a = applicant.Adventurer;
            Enqueue($"New applicant: {a.Name} ({a.Class.DisplayName} Lv {a.Level})", false);
        }

        // NOTE: Accepting / declining are the player's own actions — no toast. The office closing makes everyone leave at
        // once — one summary toast per frame instead of one per applicant.
        private void OnApplicantLeft(Applicant applicant, ApplicantEndReason reason)
        {
            switch (reason)
            {
                case ApplicantEndReason.Expired:
                    Enqueue($"{applicant.Adventurer.Name} stopped waiting", true);
                    break;
                case ApplicantEndReason.OfficeClosed when _officeClosedFrame != Time.frameCount:
                    _officeClosedFrame = Time.frameCount;
                    Enqueue("Applicants left: no Guild Master's Office", true);
                    break;
            }
        }
        
        private void OnArmorySales(int sold, long net) =>
            Enqueue(sold == 1 ? $"Armory: 1 item sold ({Signed(net)}g)" : $"Armory: {sold} items sold ({Signed(net)}g)", false);
        
        // NOTE: Full restock → normal toast; partial / nothing → error toast (the guild ran short).
        private void OnArmoryRestocked(int bought, long cost, int missing)
        {
            if (bought == 0)
                Enqueue("Armory couldn't restock (not enough gold)", true);
            else if (bought < missing)
                Enqueue($"Armory partly restocked: {bought} of {missing} items (-{cost:N0}g)", true);
            else
                Enqueue($"Armory restocked: {bought} items (-{cost:N0}g)", false);
        }
        
        // NOTE: Nobody at the guild → no toast.
        private void OnSupperServed(int served, int skipped, long gold)
        {
            if (served == 0 && skipped == 0)
                return;

            var meals = served == 1 ? "1 meal served" : $"{served} meals served";
            var text = served > 0 ? $"Tavern: {meals} (+{gold:N0}g)" : "Tavern: nobody could pay for supper";
            if (served > 0 && skipped > 0)
                text += $" · {skipped} couldn't pay";
            Enqueue(text, served == 0);
        }
        
        private void OnPotionsRefused(Quest quest, Adventurer refuser) =>
            Enqueue($"{refuser.Name} refused the potions; the party left without them", true);

        private static string Signed(long value) => value >= 0 ? $"+{value:N0}" : $"-{-value:N0}";

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