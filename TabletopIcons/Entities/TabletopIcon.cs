using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using UnityEngine;

namespace AviaryModules.TabletopIcons.Entities;

[FucineImportable("tabletopIcons")]
public class TabletopIcon(EntityData importDataForEntity, ContentImportLog log) : AbstractEntity<TabletopIcon>(importDataForEntity, log)
{
    [FucineValue]
    public string Image
    {
        get => string.IsNullOrEmpty(_image) ? Id : _image;
        set => _image = value;
    }
    
    private string? _image;
    
    [FucineConstruct(0.0, 0.0)]
    public Vector2 Position { get; set; }
    
    protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium)
    {
    }
}