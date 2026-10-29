// Reception desk: requests (left half) and applications (right half) with live counts; each opens its screen and returns here.
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Children are opened through the panel host's stack — the desk closes, and reopens when the child closes.
    public class ReceptionDeskPanel : MenuPanel
    {
        private readonly List<Button> _buttons = new();
        private Button _requests;
        private Button _applications;
        private Button _close;
        private OutlinedText _requestsDetail;
        private OutlinedText _applicationsDetail;
        private Button _lastChoice;

        protected override IReadOnlyList<Button> Buttons => _buttons;
        protected override IReadOnlyList<Button> ListButtons => System.Array.Empty<Button>();
        protected override Focusable DefaultFocus => _lastChoice ?? _requests;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _requests = documentRoot.Q<Button>("desk-requests");
            _applications = documentRoot.Q<Button>("desk-applications");
            _close = documentRoot.Q<Button>("desk-close");
            _requestsDetail = documentRoot.Q<OutlinedText>("desk-requests-detail");
            _applicationsDetail = documentRoot.Q<OutlinedText>("desk-applications-detail");

            if (_requests == null || _applications == null || _close == null || _requestsDetail == null || _applicationsDetail == null)
                return false;

            _buttons.Clear();
            _buttons.Add(_requests);
            _buttons.Add(_applications);
            _buttons.Add(_close);
            return true;
        }

        protected override void OnInitialize()
        {
            _requests.clicked += () => OpenChild(_requests, MenuPanelId.RequestBoard);
            _applications.clicked += () => OpenChild(_applications, MenuPanelId.Applications);
            _close.clicked += Close;

            // NOTE: Left/Right swaps the two halves; Up/Down walks halves → Close. Each passes the other axis through,
            // so every arrow has exactly one owner.
            AddPanelManipulator(new MenuNavigation(new VisualElement[] { _requests, _applications },
                                                   () => IsInteractable, null, NavigationAxis.Horizontal, passThroughCrossAxis: true));
            AddPanelManipulator(new MenuNavigation(new VisualElement[] { _requests, _applications, _close },
                                                   () => IsInteractable, null, passThroughCrossAxis: true));

            GameEventsRelay.OnRequestAdded.AddListener(OnRequestAdded);
            GameEventsRelay.OnRequestRemoved.AddListener(OnRequestRemoved);
            GameEventsRelay.OnQuestPosted.AddListener(OnQuestChanged);
            GameEventsRelay.OnQuestUpdated.AddListener(OnQuestChanged);
            GameEventsRelay.OnQuestPartyReady.AddListener(OnQuestChanged);
            GameEventsRelay.OnQuestEnded.AddListener(OnQuestEnded);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnRequestAdded.RemoveListener(OnRequestAdded);
            GameEventsRelay.OnRequestRemoved.RemoveListener(OnRequestRemoved);
            GameEventsRelay.OnQuestPosted.RemoveListener(OnQuestChanged);
            GameEventsRelay.OnQuestUpdated.RemoveListener(OnQuestChanged);
            GameEventsRelay.OnQuestPartyReady.RemoveListener(OnQuestChanged);
            GameEventsRelay.OnQuestEnded.RemoveListener(OnQuestEnded);
            base.OnDestroy();
        }

        // NOTE: Also runs when a child screen closes and the host reopens the desk — counts are always current.
        protected override void OnOpening() => Refresh();

        protected override void OnCancel() => Close();

        private void Close()
        {
            if (IsInteractable)
                Host.Close(Id);
        }

        private void OpenChild(Button choice, MenuPanelId child)
        {
            if (!IsInteractable)
                return;

            _lastChoice = choice;
            Host.Open(child);
        }

        private void Refresh()
        {
            int total = 0, unseen = 0;
            if (RequestManager.HasInstance)
            {
                foreach (var request in RequestManager.Instance.Requests)
                {
                    total++;
                    if (!request.IsSeen)
                        unseen++;
                }
            }

            int ready = 0, applicants = 0;
            if (QuestManager.HasInstance)
            {
                foreach (var quest in QuestManager.Instance.Quests)
                {
                    if (!quest.IsBeforeDeparture)
                        continue;
                    applicants += quest.MemberIds.Count;
                    if (quest.State == QuestState.Ready)
                        ready++;
                }
            }

            _requestsDetail.Text = ApplicationFormatter.DeskRequestsDetail(total, unseen);
            _applicationsDetail.Text = ApplicationFormatter.DeskApplicationsDetail(ready, applicants);
        }

        private void OnRequestAdded(ActiveRequest _) => RefreshIfOpen();
        private void OnRequestRemoved(ActiveRequest _, RequestEndReason __) => RefreshIfOpen();
        private void OnQuestChanged(Quest _) => RefreshIfOpen();
        private void OnQuestEnded(Quest _, QuestEndReason __) => RefreshIfOpen();

        private void RefreshIfOpen()
        {
            if (IsOpen)
                Refresh();
        }
    }
}