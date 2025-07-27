using System;
using System.Collections.Generic;
using System.Linq;
using SecretHistories.Entities;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;

namespace AviaryModules.TabletopIcons.Entities;

// ReSharper disable once ClassNeverInstantiated.Global
public class TabletopIconLink(EntityData importDateForEntity, ContentImportLog log) : AbstractEntity<TabletopIconLink>(importDateForEntity, log)
{
    protected override Type ValidateIdAs => typeof (TabletopIcon);

    [FucineSet(ValidateValueAs = typeof(Element))]
    public HashSet<string> ShowFor { get; set; } = [];

    [FucineSet(ValidateValueAs = typeof(Element))]
    public HashSet<string> Hovered { get; set; } = [];
    
    [FucineValue(DefaultValue = false)]
    public bool AlwaysShow { get; set; }
    
    public List<TabletopIcon>? MatchingIcons { get; private set; }
    
    protected override void OnPostImportForSpecificEntity(
        ContentImportLog log,
        Compendium populatedCompendium)
    {
        if (!IsInternalIcon())
        {
            var importDataForEntity = new EntityData(UnknownProperties);
            UnknownProperties.Add("id", Id);
            var entityToAdd = new TabletopIcon(importDataForEntity, log);
            populatedCompendium.TryAddEntity(entityToAdd);
            entityToAdd.OnPostImport(log, populatedCompendium);
        }
        MatchingIcons = populatedCompendium.GetEntitiesAsList<TabletopIcon>().Where((Func<TabletopIcon, bool>) (r => r.WildcardMatchId(Id))).ToList();
        if (MatchingIcons.Count != 0)
            return;
        NoonUtility.LogWarning($"Linked icon {this} has no matching tabletop icon. Possibly trying to link a tabletop icon that wasn't registered yet?");
        var showForWildcardIds = ShowFor.Where(e => e.Contains('*')).ToArray();
        foreach (var elementWildcard in showForWildcardIds)
        {
            ShowFor.Remove(elementWildcard);
            foreach (var element in populatedCompendium.GetEntitiesAsList<Element>().Where(r=>r.WildcardMatchId(elementWildcard)))
                ShowFor.Add(element.Id);
        }
        var hoveredWildcardIds = Hovered.Where(e => e.Contains('*')).ToArray();
        foreach (var elementWildcard in hoveredWildcardIds)
        {
            Hovered.Remove(elementWildcard);
            foreach (var element in populatedCompendium.GetEntitiesAsList<Element>().Where(r=>r.WildcardMatchId(elementWildcard)))
                Hovered.Add(element.Id);
        }
        return;

        bool IsInternalIcon()
        {
            return TypeInfoCache<TabletopIcon>.GetCachedFucinePropertiesForType()
                .Any(cachedFucineProperty => UnknownProperties.ContainsKey(cachedFucineProperty.LowerCaseName));
        }
    }
}