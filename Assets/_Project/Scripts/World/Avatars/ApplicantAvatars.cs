// Applicants' avatars: they walk in and wait in the office (a seat, a waiting spot, or standing inside), leave at bedtime and
// come back at dawn, walk out when declined, or they give up, and join the members' avatars when accepted.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Observer of the applicant events and the hour. The avatars live in AvatarManager's guest list (one source of
    // truth). Order 10: after AvatarManager (grid built in its Start, everyone moved in its Update) and RecruitmentManager
    // (applicants restored in its Start). On the AvatarManager object.
    [DefaultExecutionOrder(10)]
    public class ApplicantAvatars : MonoBehaviour
    {
        private const string GuestRole = "Applicant";

        private readonly ArrivalQueue _arrivals = new();
        private AvatarSettings _settings;
        private bool _isReady;
        private bool _resend;

        private static AvatarManager Avatars => AvatarManager.Instance;
        private static RecruitmentManager Recruitment => RecruitmentManager.Instance;

        private void Awake() => _settings = GameConfig.Instance.Avatars;

        private void OnEnable()
        {
            GameEventsRelay.OnApplicantArrived.AddListener(OnApplicantArrived);
            GameEventsRelay.OnApplicantLeft.AddListener(OnApplicantLeft);
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnFacilityPlaced.AddListener(OnFacilitiesChanged);
            GameEventsRelay.OnFacilityCompleted.AddListener(OnFacilitiesChanged);
            GameEventsRelay.OnFacilityRemoved.AddListener(OnFacilitiesChanged);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnApplicantArrived.RemoveListener(OnApplicantArrived);
            GameEventsRelay.OnApplicantLeft.RemoveListener(OnApplicantLeft);
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnFacilityPlaced.RemoveListener(OnFacilitiesChanged);
            GameEventsRelay.OnFacilityCompleted.RemoveListener(OnFacilitiesChanged);
            GameEventsRelay.OnFacilityRemoved.RemoveListener(OnFacilitiesChanged);
        }

        // NOTE: A loaded game's waiting applicants appear inside the office (as members appear where they were), not at the door.
        private void Start()
        {
            _isReady = AvatarManager.HasInstance && RecruitmentManager.HasInstance;
            if (!_isReady)
            {
                Debug.LogWarning("[ApplicantAvatars] Needs AvatarManager and RecruitmentManager in the scene.", this);
                return;
            }

            if (!IsOpenNow())
                return;

            var office = Recruitment.OfficeRoomId;
            foreach (var applicant in Recruitment.Applicants)
            {
                Vector2Int? at = null;
                if (Avatars.Navigator.TryRandomFreeCellIn(office, out var cell))
                    at = cell;
                Bring(applicant, at);
            }
        }

        private void Update()
        {
            if (!_isReady)
                return;

            TickArrivals(Time.deltaTime);
            if (_resend)
            {
                _resend = false;
                ResendAll();
            }
        }

        // NOTE: One at a time through the entrance (the members' arrival gap); skipped if they left, or it's closing time.
        private void TickArrivals(float deltaTime)
        {
            var gap = GameConfig.Instance.Processions.ArrivalGapSeconds;
            while (_arrivals.TryNext(deltaTime, gap, out var id))
            {
                deltaTime = 0f;
                var applicant = Recruitment.Find(id);
                if (applicant != null && IsOpenNow())
                    Bring(applicant, null);
            }
        }

        // NOTE: Spawns them (at = null → the entrance) — or turns them around if they're still walking out — then sends them
        // to the office.
        private void Bring(Applicant applicant, Vector2Int? at)
        {
            var guests = Avatars.Guests;
            if (guests.TryGet(applicant.Id, out var avatar))
            {
                if (!avatar.IsLeaving)
                    return;
                avatar.IsLeaving = false;
            }
            else
            {
                avatar = guests.Spawn(applicant.Adventurer, at);
                if (avatar == null)
                    return;
                SetRole(avatar, true);
            }

            SendToOffice(avatar);
        }

        // NOTE: A waiting chair in the office (sitting — only applicants use them), else a free cell inside (standing);
        // nothing reachable → they stand where they are (the next room change tries again).
        private static void SendToOffice(AdventurerAvatar avatar)
        {
            ActivitySpotRegistry.Release(avatar.AdventurerId);
            var office = Recruitment.OfficeRoomId;
            if (office < 0)
            {
                avatar.Stop();
                return;
            }

            if (Avatars.TryGoToSpot(avatar, SpotKind.Waiting, office))
                return;
            if (Avatars.Navigator.TryRandomFreeCellIn(office, out var cell) && Avatars.TryWalkTo(avatar, cell, null))
                return;
            avatar.Stop();
        }

        private void ResendAll()
        {
            foreach (var applicant in Recruitment.Applicants)
            {
                if (Avatars.Guests.TryGet(applicant.Id, out var avatar) && !avatar.IsLeaving)
                    SendToOffice(avatar);
            }
        }

        private void OnApplicantArrived(Applicant applicant)
        {
            if (_isReady && IsOpenNow())
                _arrivals.Enqueue(applicant.Id);
        }

        // NOTE: Accepted → their avatar joins the members' avatars where it stands (AvatarManager.AdoptGuest); declined,
        // expired or office closed → they walk out.
        private void OnApplicantLeft(Applicant applicant, ApplicantEndReason reason)
        {
            _arrivals.Remove(applicant.Id);
            if (!_isReady || !Avatars.Guests.TryGet(applicant.Id, out var avatar))
                return;

            if (reason == ApplicantEndReason.Accepted)
            {
                SetRole(avatar, false);
                Avatars.AdoptGuest(applicant.Id);
            }
            else if (!avatar.IsLeaving)
                Avatars.Guests.WalkOut(avatar);
        }

        // NOTE: Office hours = a finished office and not sleep time. Opening: absent applicants queue at the door, leavers
        // turn around. Closing: everyone walks out (queued arrivals are dropped).
        private void OnHourStarted(GameDateTime _)
        {
            if (!_isReady)
                return;

            var open = IsOpenNow();
            foreach (var applicant in Recruitment.Applicants)
            {
                var hasAvatar = Avatars.Guests.TryGet(applicant.Id, out var avatar);
                if (open)
                {
                    if (!hasAvatar)
                        _arrivals.Enqueue(applicant.Id);
                    else if (avatar.IsLeaving)
                        Bring(applicant, null);
                }
                else
                {
                    _arrivals.Remove(applicant.Id);
                    if (hasAvatar && !avatar.IsLeaving)
                        Avatars.Guests.WalkOut(avatar);
                }
            }
        }

        // NOTE: Deferred to Update — AvatarManager must rebuild the grid first (listener order isn't guaranteed).
        private void OnFacilitiesChanged(PlacedFacility _) => _resend = true;

        private bool IsOpenNow()
        {
            if (!Recruitment.AcceptsApplicants)
                return false;
            var minuteOfDay = (int)(RequestManager.NowMinutes % GameCalendar.MinutesPerDay);
            return !AvatarSchedule.IsSleepTime(minuteOfDay, _settings);
        }

        // NOTE: An applicant is labeled as one and opens Recruits when clicked; a member is back to their name and card.
        private static void SetRole(AdventurerAvatar avatar, bool isApplicant)
        {
            var interactable = avatar.GetComponent<AvatarInteractable>();
            if (interactable != null)
                interactable.SetGuest(isApplicant ? GuestRole : null, isApplicant ? MenuPanelId.Recruits : null);
        }
    }
}