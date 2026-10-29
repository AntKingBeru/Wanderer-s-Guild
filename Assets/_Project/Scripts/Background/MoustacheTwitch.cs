// Click reaction: the moustache plays the twitch pattern from GameConfig, then returns to rest.
using UnityEngine;

namespace WanderersGuild
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class MoustacheTwitch : MonoBehaviour, ICharacterReaction
    {
        private PixelStepAnimation _animation;

        private void Awake()
        {
            var sprite = GetComponent<SpriteRenderer>().sprite;
            _animation = new PixelStepAnimation(transform, sprite ? sprite.pixelsPerUnit : 100f, gameObject);
        }

        public void Play() => _animation.Play(GameConfig.Instance.TitleCharacterReactions.MoustacheTwitch);
    }
}