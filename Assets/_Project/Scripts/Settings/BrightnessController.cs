using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WanderersGuild
{
    public class BrightnessController : Singleton<BrightnessController>
    {
        private VolumeProfile _profile;
        private ColorAdjustments _colorAdjustments;

        protected override void OnSingletonAwake()
        {
            // NOTE: The profile is created at runtime so no asset is ever modified; it's destroyed with this object.
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _colorAdjustments = _profile.Add<ColorAdjustments>(true);
            _colorAdjustments.postExposure.Override(0f);

            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = GameConfig.Instance.DisplaySettings.BrightnessVolumePriority;
            volume.sharedProfile = _profile;
        }

        // Exposure in EV stops: 0 = unchanged, +1 = twice as bright, −1 = half.
        public void SetExposure(float exposure)
        {
            if (_colorAdjustments)
                _colorAdjustments.postExposure.Override(exposure);
        }

        protected override void OnDestroy()
        {
            if (_profile)
                Destroy(_profile);

            base.OnDestroy();
        }
    }
}