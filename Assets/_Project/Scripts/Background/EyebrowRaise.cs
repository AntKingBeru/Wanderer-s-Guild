// Click reaction: the clicked eyebrow rises one pixel at a time, holds, then lowers back to rest.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class EyebrowRaise : MonoBehaviour, ICharacterReaction
    {
        private readonly List<PixelStep> _steps = new();
        private PixelStepAnimation _animation;

        private void Awake()
        {
            var sprite = GetComponent<SpriteRenderer>().sprite;
            _animation = new PixelStepAnimation(transform, sprite ? sprite.pixelsPerUnit : 100f, gameObject);
        }

        public void Play()
        {
            BuildSteps(GameConfig.Instance.TitleCharacterReactions);
            _animation.Play(_steps);
        }

        // NOTE: e.g. 2 px → up 1, up 2 (held), back to 1, then rest.
        private void BuildSteps(TitleCharacterReactionSettings settings)
        {
            _steps.Clear();
            var height = settings.EyebrowRaisePixels;

            for (var pixel = 1; pixel <= height; pixel++)
            {
                var duration = pixel == height ? settings.EyebrowHoldDuration : settings.EyebrowStepDuration;
                _steps.Add(new PixelStep(new Vector2Int(0, pixel), duration));
            }

            for (var pixel = height - 1; pixel >= 1; pixel--)
                _steps.Add(new PixelStep(new Vector2Int(0, pixel), settings.EyebrowStepDuration));
        }
    }
}