namespace WanderersGuild
{
    public interface IState
    {
        //Called once when the state becomes active.
        void Enter();

        //Called once when the state is replaced or the machine shuts down.
        void Exit();
    }
}