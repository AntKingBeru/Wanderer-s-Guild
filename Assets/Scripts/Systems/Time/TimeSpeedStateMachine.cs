// Time-speed State Machine: each state supplies its sim multiplier; handles pause/resume.

using System.Collections.Generic;

#region States
// Base state - maps a TimeSpeed to the multiplier applied to elapsed time.
public abstract class TimeSpeedState
{
    public abstract TimeSpeed Speed { get; }
    public abstract float Multiplier { get; }
}

public sealed class PauseState : TimeSpeedState
{
    public override TimeSpeed Speed => TimeSpeed.Pause;
    public override float Multiplier => 0f;
}

public sealed class NormalState : TimeSpeedState
{
    public override TimeSpeed Speed => TimeSpeed.Normal;
    public override float Multiplier => 1f;
}

public sealed class FastState : TimeSpeedState
{
    public FastState(float mult) => Multiplier = mult;
    public override TimeSpeed Speed => TimeSpeed.Fast;
    public override float Multiplier { get; }
}

public sealed class VeryFastState : TimeSpeedState
{
    public VeryFastState(float mult) => Multiplier = mult;
    public override TimeSpeed Speed => TimeSpeed.VeryFast;
    public override float Multiplier { get; }
}
#endregion

public class TimeSpeedStateMachine
{
    private readonly Dictionary<TimeSpeed, TimeSpeedState> _states;
    private TimeSpeedState _lastRunning;
    private static readonly TimeSpeed[] RunningCycle ={ TimeSpeed.Normal, TimeSpeed.Fast, TimeSpeed.VeryFast };

    public TimeSpeedState Current { get; private set; }

    public TimeSpeedStateMachine(float fastMult, float veryFastMult)
    {
        _states = new Dictionary<TimeSpeed, TimeSpeedState>
        {
            {
                TimeSpeed.Pause,
                new PauseState()
            },
            {
                TimeSpeed.Normal,
                new NormalState()
            },
            {
                TimeSpeed.Fast,
                new FastState(fastMult)
            },
            {
                TimeSpeed.VeryFast,
                new VeryFastState(veryFastMult)
            }
        };
        Current = _states[TimeSpeed.Normal];
        _lastRunning = Current;
    }

    public bool SetSpeed(TimeSpeed speed)
    {
        if (Current.Speed == speed)
            return false;
        if (Current.Speed != TimeSpeed.Pause)
            _lastRunning = Current;
        Current = _states[speed];
        return true;
    }
    
    public bool TogglePause()
        => Current.Speed == TimeSpeed.Pause ? SetSpeed(_lastRunning.Speed) : SetSpeed(TimeSpeed.Pause);
    
    public bool CycleSpeed()
    {
        if (Current.Speed == TimeSpeed.Pause)
            return SetSpeed(_lastRunning.Speed);

        var idx = System.Array.IndexOf(RunningCycle, Current.Speed);
        var next = RunningCycle[(idx + 1) % RunningCycle.Length];
        return SetSpeed(next);
    }
}