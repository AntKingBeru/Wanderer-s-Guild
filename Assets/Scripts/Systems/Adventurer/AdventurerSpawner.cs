// MonoBehaviour with one job: decide WHEN a spontaneous adventurer arrival happens.
// Delegates the actual creation to AdventurerManager. Timer is driven by in-game hours
// (via TimeManager) rather than Update(), so it's unaffected by timescale changes.

using UnityEngine;

public class AdventurerSpawner : MonoBehaviour
{
    private AdventurerConfig _config;
    private float _nextArrivalGameHour;
    private bool _initialized;

    private void OnEnable()
    {
        if (GameEventRelay.Instance)
            GameEventRelay.Instance.OnHourChanged.AddListener(OnHourChanged);
    }
    
    private void OnDisable()
    {
        if (GameEventRelay.Instance)
            GameEventRelay.Instance.OnHourChanged.RemoveListener(OnHourChanged);
    }

    private void Start()
    {
        _config = GameManager.Instance ? GameManager.Instance.AdventurerConfig : null;
        if (!_config)
        {
            Debug.LogError("[AdventurerSpawner] No AdventurerConfig found on GameManager. Spawner disabled.");
            enabled = false;
            return;
        }
        ScheduleNextArrival();
    }
    
    private void OnHourChanged(int hour)
    {
        if (!_initialized || !TimeManager.Instance)
            return;

        if (TimeManager.Instance.GetTotalGameHours() >= _nextArrivalGameHour)
        {
            AdventurerManager.Instance?.CreateRandomAdventurer();
            ScheduleNextArrival();
        }
    }

    private void ScheduleNextArrival()
    {
        if (!TimeManager.Instance || !_config)
            return;

        var daysUntilNext = Random.Range(_config.ArrivalRateMinDays, _config.ArrivalRateMaxDays);
        _nextArrivalGameHour = TimeManager.Instance.GetTotalGameHours() + daysUntilNext * 24f;
        _initialized = true;
    }
}