using System.Globalization;
using Roost;
using Roost.Twins.Entities;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using UnityEngine;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace AviaryModules.Otherworlds.Entities
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class EgressLocation(EntityData importDataForEntity, ContentImportLog log)
        : AbstractEntity<EgressLocation>(importDataForEntity, log)
    {
        [FucineConstruct(0.0, 0.0)] public Vector2 Position { get; set; }

        [FucineBoolExp(DefaultValue = "false")]
        public FucineExp<bool> Shrouded { get; set; }

        protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium)
        {
        }
    }
}