// Stand-in for menu features scheduled for a later session; logs instead of acting.
using UnityEngine;

namespace WanderersGuild
{
    public sealed class PlaceholderCommand : IMenuCommand
    {
        private readonly string _featureName;

        public PlaceholderCommand(string featureName) => _featureName = featureName;

        public bool IsAvailable => true;
        public bool CanExecute => true;

        public void Execute() => Debug.Log($"[MainMenu] '{_featureName}' arrives in Session 5.");
    }
}