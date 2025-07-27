using System;
using System.Collections.Generic;
using Roost;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;

namespace AviaryModules.Otherworlds.Entities
{
    [FucineImportable("otherworlds")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class CustomOtherworld(EntityData importDataForEntity, ContentImportLog log)
        : AbstractEntity<CustomOtherworld>(importDataForEntity, log)
    {
        [FucineValue(DefaultValue = "map")]
        public string Image
        {
            get => string.IsNullOrEmpty(_image) ? Id : _image;
            set => _image = value;
        }

        private string? _image;

        [FucineKeyedList(QuickSpecProperty = "position")] public List<Egress>? Egresses { get; set; } = [];

        protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium)
        {
        }
    }
}