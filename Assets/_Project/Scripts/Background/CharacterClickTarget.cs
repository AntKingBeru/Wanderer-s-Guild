// Marks a clickable title-character part (its sprite's physics shape); runs its reaction if it has one, else just absorbs the click.
using UnityEngine;

namespace WanderersGuild
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class CharacterClickTarget : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private ICharacterReaction _reaction;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            TryGetComponent(out _reaction); // NOTE: No reaction = a blocker (e.g. the face): the click is absorbed, nothing plays.
        }

        // True if this part draws above the other (sorting layer first, then order in layer).
        public bool IsAbove(CharacterClickTarget other)
        {
            var layer = SortingLayer.GetLayerValueFromID(_renderer.sortingLayerID);
            var otherLayer = SortingLayer.GetLayerValueFromID(other._renderer.sortingLayerID);
            return layer != otherLayer ? layer > otherLayer : _renderer.sortingOrder > other._renderer.sortingOrder;
        }

        public void Click() => _reaction?.Play();
    }
}