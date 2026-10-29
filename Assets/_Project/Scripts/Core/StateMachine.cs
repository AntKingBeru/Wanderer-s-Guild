namespace WanderersGuild
{
    public sealed class StateMachine
    {
        public IState Current { get; private set; }

        // NOTE: Current is set before Enter(), so a state may safely call ChangeState from inside its own Enter().
        public void ChangeState(IState next)
        {
            if (next == null || next == Current) return;

            Current?.Exit();
            Current = next;
            Current.Enter();
        }

        //>Exits the active state. Call from the owner's OnDestroy so listeners are released.
        public void Shutdown()
        {
            Current?.Exit();
            Current = null;
        }
    }
}