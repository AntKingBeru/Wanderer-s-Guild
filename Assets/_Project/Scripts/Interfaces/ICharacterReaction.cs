// Something a title-character part does when clicked (Strategy — EyebrowRaise, MoustacheTwitch).
namespace WanderersGuild
{
    public interface ICharacterReaction
    {
        // Plays the reaction from rest; a second call restarts it.
        void Play();
    }
}