// Central access point to static game data; picks the placeholder or database provider at startup.
using UnityEngine;

namespace WanderersGuild
{
    // -100 runs after GameConfig (-101) but before default-order systems that read data in Awake.
    [DefaultExecutionOrder(-100)]
    public class DataManager : Singleton<DataManager>
    {
        [Header("Data Source")]
        [Tooltip("Use in-memory placeholder data until the databases are populated by the import tool.")]
        [SerializeField] private bool usePlaceholderData = true;

        [Header("Databases (used when placeholder is off)")]
        [SerializeField] private SpeciesDatabase speciesDatabase;
        [SerializeField] private ClassDatabase   classDatabase;
        [SerializeField] private RequestDatabase requestDatabase;

        // The active data source. Access as DataManager.Instance.Data.GetSpecies("human").
        public IDataProvider Data { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (!Equals(Instance, this))
                return;
            
            Data = BuildProvider();
        }

        private IDataProvider BuildProvider()
        {
            if (usePlaceholderData)
            {
                Debug.Log("[DataManager] Using PlaceholderDataProvider.");
                return new PlaceholderDataProvider();
            }
            
            if (!speciesDatabase || !classDatabase || !requestDatabase)
            {
                Debug.LogError("[DataManager] Databases not assigned; falling back to placeholder data.");
                return new PlaceholderDataProvider();
            }
            
            return new DatabaseDataProvider(speciesDatabase, classDatabase, requestDatabase);
        }
    }
}