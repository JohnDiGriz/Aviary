using System.Collections.Generic;
using System.Linq;
using AviaryModules.Collections;
using AviaryModules.TabletopIcons.Entities;
using Roost;
using SecretHistories.Entities;
using SecretHistories.Enums;
using SecretHistories.Events;
using SecretHistories.Fucine;
using SecretHistories.UI;
using UnityEngine;

namespace AviaryModules.TabletopIcons.Components;

public class TabletopIconsController : MonoBehaviour, ISphereCatalogueEventSubscriber
{
    private readonly Dictionary<string, TabletopIconGroup> _activeGroups = new();

    private readonly Dictionary<string, IconTriggersCache>
        _iconsInActiveGroups = new();

    private readonly Dictionary<string, TabletopIconComponent> _instantiatedIcons = new();

    private readonly struct IconTriggersCache(string groupId, TabletopIconLink link)
    {
        private readonly Bag<string> _showFor = [..link.ShowFor];

        private readonly Bag<string> _hovered = [..link.Hovered];

        private readonly HashSet<string> _alwaysShownGroups = link.AlwaysShow ? [groupId] : [];
        
        public bool HoverTriggered(string element)
        {
            return !_alwaysShownGroups.Any() && _hovered.ContainsKey(element);
        }
        
        public bool ShouldBeShown(AspectsInContext aspectsInContext)
        {
            if (_alwaysShownGroups.Any())
                return true;
            foreach (var element in _showFor)
            {
                if(aspectsInContext.AspectsExtant.ContainsKey(element))
                    return true;
            }

            return false;
        }

        public bool CacheTriggers(string groupId, TabletopIconLink link, AspectsInContext aspectsInContext,
            bool alreadyShown = false)
        {
            var needsShowing = alreadyShown;
            if (link.AlwaysShow)
            {
                _alwaysShownGroups.Add(groupId);
                needsShowing = true;
            }

            foreach (var elId in link.ShowFor)
            {
                _showFor.Add(elId);
                needsShowing = needsShowing || !aspectsInContext.AspectsExtant.ContainsKey(elId);
            }

            _hovered.AddRange(link.Hovered);
            return needsShowing;
        }

        public bool IsDeletable => _showFor.Count == 0 && _hovered.Count == 0 && _alwaysShownGroups.Count == 0;

        public bool DecacheTriggers(string groupId, TabletopIconLink link, AspectsInContext aspectsInContext,
            bool alreadyShown = false)
        {
            var alwaysShownLost = false;
            if (link.AlwaysShow)
            {
                alwaysShownLost = _alwaysShownGroups.Remove(groupId);
            }

            var triggersLost = false;
            foreach (var elId in link.ShowFor)
                triggersLost |= _showFor.Decrease(elId);

            foreach (var elId in link.Hovered)
                _hovered.Decrease(elId);
            if (!alreadyShown || _alwaysShownGroups.Any() || (!triggersLost && !alwaysShownLost)) return false;

            foreach (var elId in _showFor)
            {
                if (aspectsInContext.AspectsExtant.ContainsKey(elId))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public void Start()
    {
        Watchman.Register(this);
        Watchman.Get<HornedAxe>().Subscribe(this);
        //process groups
    }

    public void ActivateGroup(string id)
    {
        if (_activeGroups.ContainsKey(id))
            return;
        if (Watchman.Get<Compendium>().GetEntityById<TabletopIconGroup>(id) is not { } group) return;
        _activeGroups.Add(id, group);

        var aspectsInContext = Watchman.Get<HornedAxe>().GetAspectsInContext();
        for (var i = 0; i < group.Icons.Count; i++)
        {
            var icon = group.Icons[i];
            for (var j = 0; j < (icon.MatchingIcons?.Count ?? 0); j++)
            {
                var actualIcon = icon.MatchingIcons![j];

                if (!_instantiatedIcons.TryGetValue(actualIcon.Id, out var iconComponent))
                {
                    iconComponent = TabletopIconsMaster.CreateTabletopIconFromSpec(actualIcon, transform);
                    _instantiatedIcons.Add(actualIcon.Id, iconComponent);
                }

                if (!_iconsInActiveGroups.ContainsKey(actualIcon.Id))
                {
                    iconComponent.gameObject.SetActive(true);
                    iconComponent.Hide();
                }

                var needsShown = false;
                if (!_iconsInActiveGroups.TryGetValue(actualIcon.Id, out var iconSets))
                {
                    iconSets = new IconTriggersCache(group.Id, icon);
                    _iconsInActiveGroups.Add(actualIcon.Id, iconSets);
                }
                else
                    needsShown =
                        iconSets.CacheTriggers(group.Id, icon, aspectsInContext, iconComponent.IsCurrentlyShown);

                if (!iconComponent.IsCurrentlyShown && needsShown)
                    iconComponent.Show();
            }
        }
    }

    public void DeactivateGroup(string id)
    {
        if (!_activeGroups.Remove(id, out var group)) return;

        var aspectsInContext = Watchman.Get<HornedAxe>().GetAspectsInContext();
        for (var i = 0; i < group.Icons.Count; i++)
        {
            var icon = group.Icons[i];
            for (var j = 0; j < (icon.MatchingIcons?.Count ?? 0); j++)
            {
                var actualIcon = icon.MatchingIcons![j];
                if (!_instantiatedIcons.TryGetValue(actualIcon.Id, out var iconComponent))
                {
                    Birdsong.TweetLoud(
                        $"Trying to deactivate the icon {actualIcon.Id} before it was instantiated. Something is wrong!");
                    continue;
                }

                if (!_iconsInActiveGroups.TryGetValue(actualIcon.Id, out var iconSets))
                {
                    Birdsong.TweetLoud(
                        $"Trying to deactivate {actualIcon.Id} in {group.Id} but it's already inactive. Something is wrong!");
                    return;
                }

                var needsHiding =
                    iconSets.DecacheTriggers(group.Id, icon, aspectsInContext, iconComponent.IsCurrentlyShown);

                if (iconSets.IsDeletable)
                {
                    _iconsInActiveGroups.Remove(actualIcon.Id);
                    iconComponent.Hide();
                    iconComponent.gameObject.SetActive(false);
                }
                else if (iconComponent.IsCurrentlyShown && needsHiding)
                    iconComponent.Hide();
            }
        }
    }

    public void ActivateMatchingIcons(string elementId)
    {
        foreach (var icon in _iconsInActiveGroups)
        {
            if(!icon.Value.HoverTriggered(elementId)) continue;
            if (!_instantiatedIcons.TryGetValue(icon.Key, out var iconComponent))
            {
                Birdsong.TweetLoud(
                    $"Trying to check an icon ({icon.Key}) before it was instantiated. *Really* shouldn't happen.");
                continue;
            }
            
            iconComponent.ShowHover();
        }
    }

    public void DeactivateMatchingIcons(string elementId)
    {
        foreach (var icon in _iconsInActiveGroups)
        {
            if(!icon.Value.HoverTriggered(elementId)) continue;
            if (!_instantiatedIcons.TryGetValue(icon.Key, out var iconComponent))
            {
                Birdsong.TweetLoud(
                    $"Trying to check an icon ({icon.Key}) before it was instantiated. *Really* shouldn't happen.");
                continue;
            }
            
            iconComponent.HideHover();
        }
    }

    public void OnSphereChanged(SphereChangedArgs args)
    {
    }

    public void OnTokensChangedInAnySphere(SphereContentsChangedEventArgs args)
    {
        var aspectsInContext = Watchman.Get<HornedAxe>().GetAspectsInContext();
        foreach (var icon in _iconsInActiveGroups)
        {
            if (!_instantiatedIcons.TryGetValue(icon.Key, out var iconComponent))
            {
                Birdsong.TweetLoud(
                    $"Trying to check an icon ({icon.Key}) before it was instantiated. *Really* shouldn't happen.");
                continue;
            }

            if (icon.Value.ShouldBeShown(aspectsInContext))
                iconComponent.Show();
            else
                iconComponent.Hide();
        }
    }

    public void OnTokenInteractionInAnySphere(TokenInteractionEventArgs args)
    {
        if (args.PointerEventData.dragging)
            return;
        switch (args.Interaction)
        {
            case Interaction.OnPointerEntered:
                ActivateMatchingIcons(args.Payload.EntityId);
                break;
            case Interaction.OnPointerExited:
                DeactivateMatchingIcons(args.Payload.EntityId);
                break;
            default: return;
        }
    }
}