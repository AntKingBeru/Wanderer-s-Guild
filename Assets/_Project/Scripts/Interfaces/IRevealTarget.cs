namespace WanderersGuild
{
    public interface IRevealTarget
    {
        // Shows the first <paramref name="shown"/> characters of <paramref name="full"/>.
        void Show(string full, int shown);

        // Empties the target and resets any scrolling.
        void Clear();
    }
}