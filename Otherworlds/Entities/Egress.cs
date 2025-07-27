using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Roost;
using Roost.Twins.Entities;
using SecretHistories.Abstract;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using UnityEngine;
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace AviaryModules.Otherworlds.Entities
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Egress(EntityData importDataForEntity, ContentImportLog log)
        : AbstractEntity<Egress>(importDataForEntity, log)
    {
        [FucineValue(DefaultValue = "map_doorslot")]
        public string? Background { get; private set; }

        [FucineConstruct(1.000, 1.000, 1.000, 1.000)]
        public Color BackgroundColor { get; private set; }

        [FucineValue]
        public string Icon
        {
            get => string.IsNullOrEmpty(_icon) ? Id : _icon;
            set => _icon = value;
        }

        private string? _icon;

        [FucineValue(DefaultValue = "map_doorslot_active")]
        public string? Hover { get; private set; }

        [FucineConstruct(1.000, 1.000, 1.000, 1.000)]
        public Color HoverColor { get; private set; }

        [FucineConstruct(0.0, 0.0)] public Vector2 Position { get; set; }

        [FucineBoolExp(DefaultValue = "true")] public FucineExp<bool> AlwaysVisible { get; set; }

        // ReSharper disable once CollectionNeverUpdated.Global
        [FucineKeyedList(QuickSpecProperty = "position")] public List<EgressLocation> Locations { get; set; } = [];

        private static readonly Dictionary<string, string> LegacyPropertyNames = new()
        {
            { "slotbackgroundcolor", "backgroundcolor" },
            { "slothovercolor", "hovercolor" },
            { "slotbackground", "background" },
            { "slothover", "hover" },
        };

        public static void Enact()
        {
            Machine.AddImportMolding<Egress>(ConvertLegacyProperties);
        }
        
        private static void ConvertLegacyProperties(EntityData egressEntityData)
        {
            foreach (var pair in LegacyPropertyNames)
            {
                if (!egressEntityData.ValuesTable.ContainsKey(pair.Key) ||
                    egressEntityData.ValuesTable.ContainsKey(pair.Value)) continue;
                egressEntityData.ValuesTable[pair.Value] = egressEntityData.ValuesTable[pair.Key];
                egressEntityData.ValuesTable.Remove(pair.Key);
            }
        }
        
        protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium)
        {
        }
    }
}