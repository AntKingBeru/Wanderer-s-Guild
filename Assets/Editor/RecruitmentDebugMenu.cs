// Play-Mode debug menu for recruitment: open the screen, force an applicant, accept / decline the first, expire, log.
using UnityEditor;
using UnityEngine;
using WanderersGuild;

namespace Editor
{
    public static class RecruitmentDebugMenu
    {
        private const string MenuRoot = "Tools/Wanderer's Guild/Debug/Recruitment/";

        // NOTE: Stand-in for clicking the office in the world (it calls the same GamePanelHost.Open).
        [MenuItem(MenuRoot + "Open Recruits")]
        private static void OpenRecruits()
        {
            var host = Object.FindAnyObjectByType<GamePanelHost>();
            if (!host || host.IsBusy || host.HasOpenPanel)
            {
                Debug.LogWarning("[Recruitment] No idle GamePanelHost in the scene (close any open panel first).");
                return;
            }
            host.Open(MenuPanelId.Recruits);
        }

        [MenuItem(MenuRoot + "Force Applicant")]
        private static void ForceApplicant()
        {
            if (!RecruitmentManager.Instance.AcceptsApplicants)
            {
                Debug.Log("[Recruitment] No finished Guild Master's Office — arrivals join directly.");
                return;
            }
            if (!AdventurerManager.Instance.ForceArrival())
                Debug.Log($"[Recruitment] Nobody arrived (waiting list full: {RecruitmentManager.Instance.Applicants.Count}/{RecruitmentManager.Instance.MaxWaiting}).");
        }

        [MenuItem(MenuRoot + "Accept First")]
        private static void AcceptFirst()
        {
            var list = RecruitmentManager.Instance.Applicants;
            if (list.Count == 0)
            {
                Debug.Log("[Recruitment] Nobody is waiting.");
                return;
            }

            var first = list[0];
            if (!RecruitmentManager.Instance.Accept(first.Id))
                Debug.Log($"[Recruitment] Couldn't accept {first.Adventurer.Name} — the guild is full ({AdventurerManager.Instance.Adventurers.Count}/{AdventurerManager.Instance.Capacity}).");
        }

        [MenuItem(MenuRoot + "Decline First")]
        private static void DeclineFirst()
        {
            var list = RecruitmentManager.Instance.Applicants;
            if (list.Count > 0)
                RecruitmentManager.Instance.Decline(list[0].Id);
        }

        [MenuItem(MenuRoot + "Expire Everyone Now")]
        private static void ExpireAll() => RecruitmentManager.Instance.ExpireAllNow();

        [MenuItem(MenuRoot + "Log Applicants")]
        private static void LogApplicants()
        {
            var now = RequestManager.NowMinutes;
            foreach (var applicant in RecruitmentManager.Instance.Applicants)
            {
                var a = applicant.Adventurer;
                Debug.Log($"#{a.Id} {a.Name} — {a.Species.DisplayName} {a.Class.DisplayName} Lv {a.Level}, {a.Stats}; " +
                          $"leaves in {RequestFormatter.TimeLeft(System.Math.Max(0L, applicant.ExpiresAtMinutes - now))}");
            }
            Debug.Log($"[Recruitment] {RecruitmentManager.Instance.Applicants.Count}/{RecruitmentManager.Instance.MaxWaiting} waiting " +
                      $"(office {(RecruitmentManager.Instance.AcceptsApplicants ? "active" : "inactive")}).");
        }

        [MenuItem(MenuRoot + "Open Recruits", true)]
        [MenuItem(MenuRoot + "Force Applicant", true)]
        [MenuItem(MenuRoot + "Accept First", true)]
        [MenuItem(MenuRoot + "Decline First", true)]
        [MenuItem(MenuRoot + "Expire Everyone Now", true)]
        [MenuItem(MenuRoot + "Log Applicants", true)]
        private static bool CanUse() => EditorApplication.isPlaying && RecruitmentManager.HasInstance && AdventurerManager.HasInstance;
    }
}