// Recruits screen (the Guild Master's Office): applicants waiting (soonest to leave first) + details; Decline / Accept.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class RecruitsPanel : MenuPanel
    {
        private const string EmptyMessage = "No applicants right now.";
        private const int RowPool = 10; // More than any office level allows by default; extra applicants aren't listed.

        private readonly List<Button> _buttons = new();
        private readonly List<Button> _rowButtons = new();
        private readonly List<VisualElement> _navigationOrder = new();
        private readonly List<RecruitRowView> _rows = new();
        private readonly List<Applicant> _sorted = new();
        private OutlinedText _title;
        private ScrollView _list;
        private OutlinedText _listEmpty;
        private RecruitDetailsView _details;
        private Button _close;
        private int _selectedId = -1;
        private int _selectedIndex;
        private bool _warnedPool;

        protected override IReadOnlyList<Button> Buttons => _buttons;
        protected override IReadOnlyList<Button> ListButtons => _rowButtons;
        protected override Focusable DefaultFocus => (Focusable)ButtonFor(_selectedId) ?? _close;

        private static long Now => RequestManager.NowMinutes;

        protected override bool OnBind(VisualElement documentRoot)
        {
            _title = documentRoot.Q<OutlinedText>("rec-title");
            _list = documentRoot.Q<ScrollView>("rec-list");
            _listEmpty = documentRoot.Q<OutlinedText>("rec-list-empty");
            _close = documentRoot.Q<Button>("rec-close");
            _details = RecruitDetailsView.Bind(documentRoot);

            if (_title == null || _list == null || _listEmpty == null || _close == null || _details == null)
                return false;

            for (var i = 0; i < RowPool; i++)
            {
                RecruitRowView row = new(clicked => Select(clicked.Applicant));
                row.Button.RegisterCallback<FocusEvent>(_ => _list.ScrollTo(row.Button));
                _list.Add(row.Button);
                _rows.Add(row);
                _rowButtons.Add(row.Button);
            }

            _buttons.Clear();
            _buttons.Add(_details.Decline);
            _buttons.Add(_details.Accept);
            _buttons.Add(_close);

            // NOTE: Up/Down walks rows → Decline → Accept → Close; hidden/disabled ones are skipped.
            _navigationOrder.Clear();
            _navigationOrder.AddRange(_rowButtons);
            _navigationOrder.Add(_details.Decline);
            _navigationOrder.Add(_details.Accept);
            _navigationOrder.Add(_close);
            return true;
        }

        protected override void OnInitialize()
        {
            _details.Decline.clicked += Decline;
            _details.Accept.clicked += Accept;
            _close.clicked += Close;

            AddPanelManipulator(new MenuNavigation(_navigationOrder, () => IsInteractable, null));

            GameEventsRelay.OnApplicantArrived.AddListener(OnApplicantArrived);
            GameEventsRelay.OnApplicantLeft.AddListener(OnApplicantLeft);
            GameEventsRelay.OnAdventurerJoined.AddListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerCapacityChanged.AddListener(OnCapacityChanged);
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnApplicantArrived.RemoveListener(OnApplicantArrived);
            GameEventsRelay.OnApplicantLeft.RemoveListener(OnApplicantLeft);
            GameEventsRelay.OnAdventurerJoined.RemoveListener(OnAdventurerEvent);
            GameEventsRelay.OnAdventurerCapacityChanged.RemoveListener(OnCapacityChanged);
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            base.OnDestroy();
        }

        protected override void OnOpening() => Refresh();

        protected override void OnCancel() => Close();

        private void Close()
        {
            if (IsInteractable)
                Host.Close(Id);
        }

        // NOTE: Selection only changes on click/Submit (not on hover-focus).
        private void Select(Applicant applicant)
        {
            if (!IsInteractable || applicant == null)
                return;

            _selectedId = applicant.Id;
            Refresh();
        }

        // NOTE: Accept is re-checked by RecruitmentManager (refused while full); the applicant events refresh the screen.
        private void Accept()
        {
            if (!IsInteractable || !RecruitmentManager.HasInstance)
                return;
            RecruitmentManager.Instance.Accept(_selectedId);
            RefreshAndKeepFocus();
        }

        private void Decline()
        {
            if (!IsInteractable || !RecruitmentManager.HasInstance)
                return;
            RecruitmentManager.Instance.Decline(_selectedId);
            RefreshAndKeepFocus();
        }

        // NOTE: Soonest to leave first, then id. Rows past the pool aren't listed (warned once).
        private void Refresh()
        {
            _sorted.Clear();
            var slots = 0;
            if (RecruitmentManager.HasInstance)
            {
                _sorted.AddRange(RecruitmentManager.Instance.Applicants);
                slots = RecruitmentManager.Instance.MaxWaiting;
            }
            _sorted.Sort((a, b) => a.ExpiresAtMinutes != b.ExpiresAtMinutes
                ? a.ExpiresAtMinutes.CompareTo(b.ExpiresAtMinutes)
                : a.Id.CompareTo(b.Id));

            if (_sorted.Count > RowPool && !_warnedPool)
            {
                _warnedPool = true;
                Debug.LogWarning($"[RecruitsPanel] {_sorted.Count} applicants but only {RowPool} rows; raise RowPool.", this);
            }

            var selected = ResolveSelection();
            _title.Text = RecruitFormatter.Title(_sorted.Count, slots);

            for (var i = 0; i < _rows.Count; i++)
            {
                if (i < _sorted.Count)
                    _rows[i].Show(_sorted[i], Now, _sorted[i].Id == _selectedId);
                else
                    _rows[i].Hide();
            }
            _listEmpty.style.display = _sorted.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            if (selected != null && AdventurerManager.HasInstance)
                _details.Show(selected, Now, AdventurerManager.Instance.Adventurers.Count, AdventurerManager.Instance.Capacity);
            else
                _details.ShowEmpty(EmptyMessage);
        }

        // NOTE: A focused row or button that just got hidden/disabled would strand the keyboard — move to the default target.
        private void RefreshAndKeepFocus()
        {
            Refresh();

            var focused = _close.focusController?.focusedElement;
            if (focused is VisualElement { enabledInHierarchy: false })
                DefaultFocus?.Focus();
        }

        private Applicant ResolveSelection()
        {
            var visible = Mathf.Min(_sorted.Count, RowPool);
            for (var i = 0; i < visible; i++)
            {
                if (_sorted[i].Id != _selectedId)
                    continue;

                _selectedIndex = i;
                return _sorted[i];
            }

            if (visible == 0)
            {
                _selectedId = -1;
                _selectedIndex = 0;
                return null;
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, visible - 1);
            _selectedId = _sorted[_selectedIndex].Id;
            return _sorted[_selectedIndex];
        }

        private Button ButtonFor(int applicantId)
        {
            return (from row in _rows where row.Applicant != null && row.Applicant.Id == applicantId select row.Button).FirstOrDefault();
        }

        private void OnApplicantArrived(Applicant _) => RefreshIfOpen();
        private void OnApplicantLeft(Applicant _, ApplicantEndReason __) => RefreshIfOpen();
        private void OnAdventurerEvent(Adventurer _) => RefreshIfOpen();
        private void OnCapacityChanged(int _) => RefreshIfOpen();
        private void OnHourStarted(GameDateTime _) => RefreshIfOpen();

        private void RefreshIfOpen()
        {
            if (IsOpen)
                RefreshAndKeepFocus();
        }
    }
}