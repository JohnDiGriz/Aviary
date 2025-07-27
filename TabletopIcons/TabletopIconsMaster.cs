using AviaryModules.Helpers;
using AviaryModules.TabletopIcons.Components;
using AviaryModules.TabletopIcons.Entities;
using Roost;
using Roost.Piebald;
using SecretHistories.Services;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AviaryModules.TabletopIcons;

public static class TabletopIconsMaster
{
    private static GameObject? _iconPrefab;
    
    internal static void Enact()
    {
        
    }

    public static TabletopIconComponent CreateTabletopIconFromSpec(TabletopIcon spec, Transform parent)
    {
        if (_iconPrefab == null)
            throw Birdsong.Cack("Trying to create an icon before setting up pseudo-prefab");
        var icon = Object.Instantiate(_iconPrefab, parent, false);
        icon.name = spec.Id;
        icon.transform.position = new Vector3(spec.Position.x, spec.Position.y, 0);
        if(spec.Localizable)
            icon.GetComponent<Image>().SetLocalizedUISprite(Watchman.Get<Config>().GetCurrentCulture().Id,
                spec.Image);
        else
            icon.GetComponent<Image>().SetUISprite(spec.Image);

        var coreScript = icon.GetComponent<TabletopIconComponent>();
        coreScript.IconSpec = spec;
        return coreScript;
    }
}