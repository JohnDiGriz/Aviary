using System.Collections.Generic;
using System.Linq;
using AviaryModules.ImprovedFX;
using Roost.Twins.Entities;
using Roost.World.Recipes;
using SecretHistories.Entities;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;

namespace AviaryModules.TabletopIcons.Entities;

[FucineImportable("tabletopGroups")]
public class TabletopIconGroup(EntityData importDataForEntity, ContentImportLog log) 
    : AbstractEntity<TabletopIconGroup>(importDataForEntity, log)
{

    [FucineSet(ValidateValueAs = typeof(Legacy))]
    public HashSet<string> Legacies { get; set; } = [];

    [FucineList] public List<TabletopIconLink> Icons { get; set; } = [];

    protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium)
    {
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}