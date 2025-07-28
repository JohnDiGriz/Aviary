using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using AviaryModules.Helpers;
using AviaryModules.Otherworlds.Components;
using AviaryModules.Otherworlds.Entities;
using HarmonyLib;
using Roost;
using Roost.Piebald;
using Roost.Twins;
using Roost.World.Recipes;
using Roost.World.Recipes.Entities;
using SecretHistories.Entities;
using SecretHistories.Fucine;
using SecretHistories.Infrastructure;
using SecretHistories.Logic;
using SecretHistories.Otherworlds;
using SecretHistories.Services;
using SecretHistories.Spheres;
using SecretHistories.Tokens.Payloads;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AviaryModules.Otherworlds
{
    internal static class CustomOtherworldsMaster
    {
        private static readonly Action<Otherworld, string> OtherworldIdField =
            "editableId".BuildSet<Otherworld, string>();

        private static readonly Func<Otherworld, EnRouteSphere> OtherworldEnRouteField =
            "otherworldSpecificEnRouteSphere".BuildGet<Otherworld, EnRouteSphere>();

        private static readonly PrecompilationHelpers.FullAccessor<Otherworld, List<OtherworldDominion>>
            OtherworldDominionsField =
                "_dominions".BuildFullAccessor<Otherworld, List<OtherworldDominion>>();

        private static readonly Func<OtherworldDominion, List<Sphere>>
            OtherworldDominionSpheresField =
                "_spheres".BuildGet<OtherworldDominion, List<Sphere>>();

        private static readonly PrecompilationHelpers.FullAccessor<OtherworldDominion, string>
            OtherworldDominionIdField =
                "EditableIdentifier".BuildFullAccessor<OtherworldDominion, string>();

        private static readonly Action<OtherworldDominion, bool> OtherworldDominionAlwaysVisibleField =
            "IsAlwaysVisible".BuildSet<OtherworldDominion, bool>();

        private static readonly Func<Situation, HashSet<Sphere>> SituationSpheresField =
            "_spheres".BuildGet<Situation, HashSet<Sphere>>();

        private const string GRAND_EFFECTS = nameof(GrandEffects);

        private const string PORTAL_MESSAGES = "messages";

        private static readonly Dictionary<(string otherworld, string location),
            EgressPseudoSituation> EgressSituations = new();

        private static readonly Dictionary<string, List<(OtherworldDominion egress, Egress spec)>> Egresses = new();

        private static readonly
            Dictionary<(string otherworldId, string egressId), List<(OutputSphere location, EgressLocation spec)>>
            EgressLocations = new();

        private static List<Otherworld>? _otherworlds;

        private static Numa? _numa;

        private static readonly HashSet<string> RegisteredOtherworlds = [];

        private static GameObject? _locationOutputPrefab;

        private static GameObject? _locationStoragePrefab;

        private static GameObject? _egressPrefab;

        private static GameObject? _otherworldPrefab;

        private static bool _destroyedOldOtherworld;


        internal static void Enact()
        {
            Egress.Enact();
            Machine.ClaimProperty<LinkedRecipeDetails, Dictionary<string, string>>(PORTAL_MESSAGES);
            Machine.Patch<Numa>(
                original: nameof(Numa.Awake),
                prefix: typeof(CustomOtherworldsMaster).GetMethodInvariant(nameof(DestroyVanillaOtherworld)));
            
            Machine.Patch<Numa>(
                original: nameof(Numa.OpenIngress),
                prefix: typeof(CustomOtherworldsMaster).GetMethodInvariant(nameof(SetExpressionValues))
                );

            Machine.Patch<Ingress>(
                original: nameof(Ingress.FirstHeartbeat),
                prefix: typeof(CustomOtherworldsMaster).GetMethodInvariant(nameof(EnsureOtherworldExists)));

            Machine.Patch<Otherworld>(
                original: nameof(Otherworld.Prepare),
                prefix: typeof(CustomOtherworldsMaster).GetMethodInvariant(nameof(PrepareOtherworldReworked)));

            Machine.Patch(
                original: typeof(Portal).GetPropertyInvariant(nameof(Portal.Icon)).GetGetMethod(),
                postfix: typeof(CustomOtherworldsMaster).GetMethodInvariant(nameof(SubstituteIdForPortalIcon)));

            Machine.Patch<Otherworld>(
                original: "EnactConsequences",
                transpiler: typeof(CustomOtherworldsMaster).GetMethodInvariant(nameof(ConsequencesTranspiler))
            );
            
            AtTimeOfPower.CompendiumLoad.Schedule(ReloadOtherworlds, PatchType.Postfix);
        }

        // ReSharper disable twice InconsistentNaming

        private static IEnumerable<CodeInstruction> ConsequencesTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var keyCodes = new List<CodeInstruction>()
            {
                new(OpCodes.Ldloc_3),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(Otherworld).GetPropertyInvariant("Id").GetGetMethod()),
                new(OpCodes.Ldloc_1),
                new(OpCodes.Call, typeof(CustomOtherworldsMaster).GetMethodInvariant(nameof(DealCardsFromGrandEffects)))
            };

            return instructions.ReplaceSegment(
                code => code.Calls(typeof(FucinePathPart).GetMethodInvariant(nameof(FucinePathPart.TrimSpherePrefix))),
                code => code.opcode == OpCodes.Endfinally, keyCodes, false, true);
        }

        private static void ReloadOtherworlds(Compendium compendiumToPopulate)
        {
            if(!_destroyedOldOtherworld)
                return;
            var idsToRemove = new int[_otherworlds!.Count];
            var removeCount = 0;
            for(var i =0; i < _otherworlds.Count; i++) {
                var otherworldComponent = _otherworlds[i];
                if (compendiumToPopulate.GetEntityById<CustomOtherworld>(otherworldComponent.EntityId) is { } otherworld)
                {
                    otherworldComponent.UpdateFromSpec(otherworld);
                }
                else
                {
                    RegisteredOtherworlds.Remove(otherworldComponent.EntityId);
                    otherworldComponent.ActualRetire();
                    Object.Destroy(otherworldComponent.gameObject);
                    idsToRemove[removeCount++] = i;
                }
            }

            for (var i = removeCount-1; i >= 0; i--)
            {
                _otherworlds.RemoveAt(idsToRemove[i]);
            }
        }

        public static void DealCardsFromGrandEffects(string targetSphere, Recipe recipe, string otherworldId,
            LinkedRecipeDetails consequence)
        {
            Birdsong.TweetQuiet($"Trying to execute a consequence in location {targetSphere} of {otherworldId}.");
            if (!EgressSituations.TryGetValue((otherworldId, targetSphere), out var spheres)
                || spheres.Output is null || spheres.Storage is null)
            {
                Birdsong.TweetLoud("Failed to find indicated sphere.");
                return;
            }

            var component = spheres.Output.gameObject.GetComponent<CanvasGroupFader>();
            if (component != null)
                component.Show();
            if (recipe.TryRetrieveProperty(GRAND_EFFECTS, out GrandEffects grandEffects))
            {
                spheres.State.Enter(spheres);
                Crossroads.MarkLocalSituation(spheres);
                grandEffects.RunGrandEffects(spheres.Storage, false);
                RecipeLinkMaster._tempLinks.Remove(spheres);
                RecipeExecutionBuffer.ApplyDecorativeEffects();
                RecipeExecutionBuffer.ApplyRecipeInductions();
                Crossroads.ClearRedirectSpheres();
                spheres.State.Exit(spheres);
            }
            else
            {
                if (!recipe.TryRetrieveProperty("deckeffects", out Dictionary<string, string> deckEffects))
                    deckEffects = recipe.DeckEffects;
                if (deckEffects is null || deckEffects.Count == 0)
                    return;
                var fromTable = Watchman.Get<DealersTable>();
                foreach (var effect in deckEffects)
                {
                    var value = RichEffectsParser.GetPoorContextValue(effect.Value,
                        RichEffectsParser.PoorContext.Otherworld);
                    for (var i = 0; i < value; i++)
                        spheres.Storage.AcceptToken(Dealer.Deal(effect.Key, fromTable), Context.Unknown());
                }
            }
            Birdsong.TweetQuiet($"Finished executing effects in location {targetSphere} of {otherworldId}. Preparing to move elements to output.");
            Watchman.Get<Xamanek>().CompleteItinerarieFor(spheres.Storage);
            var outputRect = spheres.Output.GetRectTransform();
            var tokens = spheres.Storage.Tokens;
            outputRect.sizeDelta = new Vector2(tokens.Count * 75f * 1.75f, outputRect.sizeDelta.y);
            if (consequence.TryRetrieveProperty<Dictionary<string, string>>(PORTAL_MESSAGES, out var messages))
                foreach (var storageToken in tokens)
                    if (messages.TryGetValue(storageToken.PayloadEntityId, out var message))
                        storageToken.Payload.SetIllumination("mansusjournal", message);
            spheres.Output.AcceptTokens(tokens, Context.Unknown());
            Birdsong.TweetQuiet($"Successfully executed a consequence in location {targetSphere} of {otherworldId}.");
        }

        public static void SubstituteIdForPortalIcon(Portal __instance, ref string __result)
        {
            if (__result == ".")
                __result = __instance.Id;
        }

        public static void DestroyVanillaOtherworld(Numa __instance, ref List<Otherworld> ___Otherworlds)
        {
            Birdsong.TweetQuiet("Destroying vanilla Mansus and preparing templates.");
            var vanillaMansus = ___Otherworlds.FirstOrDefault();
            ___Otherworlds.Clear();
            _otherworlds = ___Otherworlds;
            _numa = __instance;
            if (vanillaMansus is null) return;
            var dominion = vanillaMansus.transform.GetChild(1);
            var outputFound = false;
            for (var i = 0; i < dominion.childCount; i++)
            {
                var child = dominion.GetChild(i);
                if (outputFound && child.childCount == 0)
                {
                    child.gameObject.SetActive(false);
                    child.SetParent(null);
                    Object.Destroy(child.gameObject);
                    i--;
                }
                else
                {
                    Object.DestroyImmediate(child.GetComponent<PermanentSphereSpec>());
                    if (child.childCount != 0)
                    {
                        child.localPosition = Vector3.zero;
                        for (var j = 0; j < child.childCount; j++)
                        {
                            var transform = child.GetChild(j).GetComponent<RectTransform>();
                            transform.localPosition = Vector3.zero;
                            transform.localScale = Vector3.one;
                        }
                    }
                    else
                    {
                        _locationOutputPrefab = child.CreatePseudoPrefab();
                        var storageSphereGO = new GameObject("StorageSphere", typeof(RectTransform))
                        {
                            transform = { localPosition = Vector3.zero }
                        };
                        storageSphereGO.SetActive(false);
                        storageSphereGO.AddComponent<EgressStorageSphere>();
                        _locationStoragePrefab = storageSphereGO;
                        outputFound = true;
                        i--;
                        Birdsong.TweetQuiet("Created egress location templates.");
                    }
                }
            }

            _egressPrefab = dominion.CreatePseudoPrefab();
            Birdsong.TweetQuiet("Created egress template.");
            while (vanillaMansus.transform.childCount > 1)
            {
                var child = vanillaMansus.transform.GetChild(0);
                child.gameObject.SetActive(false);
                child.SetParent(null);
                Object.Destroy(child.gameObject);
            }

            if (OtherworldDominionsField.Get(vanillaMansus) is { } dominions)
                dominions.Clear();
            Object.DestroyImmediate(vanillaMansus.transform.GetChild(0)
                .GetComponent<PermanentSphereSpec>());
            _otherworldPrefab = vanillaMansus.transform.CreatePseudoPrefab();
            Birdsong.TweetQuiet("Created otherworld template.");
            _destroyedOldOtherworld = true;
        }

        public static bool SetExpressionValues(Ingress ingress)
        {
            var otherworldId = ingress.GetOtherworldId();
            Birdsong.TweetQuiet($"Opening {otherworldId} and ensuring that expression values are up to date.");
            if (!RegisteredOtherworlds.Contains(otherworldId))
            {
                Birdsong.TweetLoud($"Trying to open unregistered otherworld {otherworldId}. Cancelling the portal");
                return false;
            }

            if (!Egresses.TryGetValue(otherworldId, out var egresses))
            {
                Birdsong.TweetLoud($"Egresses are missing in otherworld {otherworldId}. Cancelling the portal");
                return false;
            }

            foreach (var egress in egresses)
            {
                OtherworldDominionAlwaysVisibleField(egress.egress, egress.spec.AlwaysVisible.value);
            }
            Birdsong.TweetQuiet("Set egress visibility values.");

            if (!EgressLocations.TryGetValue((otherworldId, ingress.GetEgressId()), out var locations))
            {
                Birdsong.TweetLoud($"Trying to open non-existing egress {ingress.GetEgressId()} in {otherworldId}. Cancelling the portal");
                return false;
            }

            foreach (var location in locations)
            {
                location.location.AlwaysShroudIncomingTokens = location.spec.Shrouded.value;
            }
            Birdsong.TweetQuiet("Set location shroud values");
            return true;
        }
        
        public static void EnsureOtherworldExists(Ingress __instance)
        {
            if (RegisteredOtherworlds.Contains(__instance.GetOtherworldId())) return;
            Birdsong.TweetQuiet($"Otherworld {__instance.GetOtherworldId()} does not yet exist. Creating one now.");
            var otherworldSpec = Watchman.Get<Compendium>()
                .GetEntityById<CustomOtherworld>(__instance.GetOtherworldId());
            if (otherworldSpec is null)
                throw Birdsong.Cack($"Otherworld with id {__instance.GetOtherworldId()} is not defined.");
            if (_otherworlds is null)
                throw Birdsong.Cack("Trying to create an otherworld before destroying vanilla one");
            var otherworld = CreateOtherworldFromSpec(otherworldSpec);
            _otherworlds.Add(otherworld);
            RegisteredOtherworlds.Add(otherworldSpec.Id);

            otherworld.Prepare();
            Birdsong.TweetQuiet($"Created and prepared {otherworld.Id}.");
        }

        public static bool PrepareOtherworldReworked(Otherworld __instance,
            EnRouteSphere ___otherworldSpecificEnRouteSphere)
        {
            foreach (var dominion in __instance.Dominions)
                if (dominion != null)
                    dominion.RegisterFor(__instance);

            ___otherworldSpecificEnRouteSphere.SetContainer(__instance);
            return false;
        }

        private static void UpdateFromSpec(this Otherworld otherworld, CustomOtherworld spec)
        {
            if (_otherworldPrefab == null || _numa == null)
                throw Birdsong.Cack("Trying to create an otherworld before destroying vanilla one");
            otherworld.GetComponent<Image>().SetLocalizedUISprite(Watchman.Get<Config>().GetCurrentCulture().Id,
                spec.Image);
            if (OtherworldDominionsField.Get(otherworld) is not { } dominions)
            {
                dominions = [];
                OtherworldDominionsField.Set(otherworld, dominions);
            }
            Birdsong.TweetQuiet($"Created core {spec.Id}. Creating egresses.");
            var instantiatedIndices = new bool[spec.Egresses!.Count];
            var egressList = Egresses[spec.Id];
            var indicesToRemove = new int[egressList.Count];
            var removeCount = 0;
            for (var i = 0; i < egressList.Count; i++)
            {
                var dominion = egressList[i];
                if (spec.Egresses?.FindIndex(e => e.Id == dominion.egress.Identifier) is { } egressIndex)
                {
                    dominion.egress.UpdateFromSpec(spec.Id, spec.Egresses[egressIndex]);
                    egressList[i] = (dominion.egress, spec.Egresses[egressIndex]);
                    instantiatedIndices[egressIndex] = true;
                }
                else
                {
                    dominion.egress.RetireFrom(otherworld);
                    indicesToRemove[removeCount++] = i;
                    Object.Destroy(dominion.egress.gameObject);
                }
            }

            for (var i = removeCount - 1; i >= 0; i--)
            {
                egressList.RemoveAt(indicesToRemove[i]);
            }

            for (var i = 0; i < spec.Egresses!.Count; i++)
                if (!instantiatedIndices[i])
                    dominions.Add(CreateOtherworldDominionFromSpec(spec.Id, spec.Egresses[i], otherworld.transform));
            
            if (OtherworldEnRouteField(otherworld) is { } enRouteSphere)
                enRouteSphere.transform.SetAsLastSibling();
            else
                Birdsong.TweetLoud($"Missing enroute sphere in the otherworld {spec.Id}.");
        }

        private static void ActualRetire(this Otherworld otherworld)
        {
            otherworld.gameObject.SetActive(false);
            otherworld.transform.SetParent(null);
            if (OtherworldDominionsField.Get(otherworld) is { } dominions)
                for (var i = 0; i < dominions.Count; i++)
                {
                    dominions[i].RetireFrom(otherworld);
                    Object.Destroy(dominions[i].gameObject);
                }

            if (OtherworldEnRouteField(otherworld) is { } enRouteSphere)
                Watchman.Get<HornedAxe>().DeregisterSphere(enRouteSphere);
            Egresses.Remove(otherworld.Id);
        }

        private static void UpdateFromSpec(this OtherworldDominion dominion, string otherworldId, Egress spec)
        {
            dominion.transform.position = new Vector3(spec.Position.x, spec.Position.y, 0);
            OtherworldDominionAlwaysVisibleField(dominion, spec.AlwaysVisible.value);
            if (OtherworldDominionSpheresField(dominion) is not { } locations)
            {
                throw Birdsong.Cack("Failed to get dominion's spheres.");
            }
            dominion.EgressSphere.doorColor.SetUISprite(spec.Background!, spec.BackgroundColor);
            dominion.EgressSphere.slotGlow.GetComponent<Image>().SetUISprite(spec.Hover!,
                spec.HoverColor);
            dominion.EgressSphere.defaultBackgroundColor = spec.BackgroundColor;
            dominion.EgressSphere.transform.Find("Icon").GetComponent<Image>().SetUISprite(spec.Icon);
            if (!EgressLocations.TryGetValue((otherworldId, spec.Id), out var locationList))
            {
                locationList = [];
                EgressLocations.Add((otherworldId, spec.Id), locationList);
            }
            var instantiatedIndices = new bool[spec.Locations.Count];
            var indicesToRemove = new int[locationList.Count];
            var removeCount = 0;
            for (var i = 0; i < locationList.Count; i++)
            {
                var location = locationList[i];
                if (spec.Locations?.FindIndex(e => e.Id == location.spec.Id) is { } locationIndex)
                {
                    location.location.UpdateFromSpec( 
                        EgressSituations[(otherworldId, spec.Locations[i].Id)].Storage!, spec.Locations[locationIndex]);
                    locationList[i] = (location.location, spec.Locations[locationIndex]);
                    instantiatedIndices[locationIndex] = true;
                }
                else
                {
                    location.location.RetireFrom(otherworldId);
                    indicesToRemove[removeCount++] = i;
                    Object.Destroy(location.location.gameObject);
                }
            }

            for (var i = removeCount - 1; i >= 0; i--)
            {
                locationList.RemoveAt(indicesToRemove[i]);
            }
            
            for (var i = 0; i < spec.Locations!.Count; i++)
            {
                if (instantiatedIndices[i]) continue;
                var locationResult =
                    CreateEgressLocationFromSpec(spec.Id, spec.Locations[i], dominion.transform);
                locations.Add(locationResult.output);
                EgressSituations[(otherworldId, spec.Locations[i].Id)] = new EgressPseudoSituation(otherworldId,
                    locationResult.storage,
                    locationResult.output, SituationSpheresField);

                locationList.Add((locationResult.output, spec.Locations[i]));
            }
        }

        private static void RetireFrom(this OtherworldDominion dominion, Otherworld otherworld)
        {
            dominion.gameObject.SetActive(false);
            dominion.transform.SetParent(null);
            Watchman.Get<HornedAxe>().DeregisterSphere(dominion.EgressSphere);
            if (OtherworldDominionSpheresField(dominion) is  { } locations)
            {
                for(var i = 0;i<locations.Count;i++)
                    if (locations[i] is OutputSphere location)
                    {
                        location.RetireFrom(otherworld.EntityId);
                        Object.Destroy(location.gameObject);
                    }
            }

            EgressLocations.Remove((otherworld.EntityId, dominion.Identifier));
        }

        private static void UpdateFromSpec(this OutputSphere location, EgressStorageSphere storage, EgressLocation spec)
        {
            location.transform.position = new Vector3(spec.Position.x, spec.Position.y);
            location.AlwaysShroudIncomingTokens = spec.Shrouded.value;
            storage.transform.position = new Vector3(spec.Position.x, spec.Position.y);
        }

        private static void RetireFrom(this OutputSphere location, string otherworldId)
        {
            location.gameObject.SetActive(false);
            location.transform.SetParent(null);
            Watchman.Get<HornedAxe>().DeregisterSphere(location);
            var egressSituation = EgressSituations[(otherworldId, location.Id)];
            egressSituation.Storage!.gameObject.SetActive(false);
            egressSituation.Storage.transform.SetParent(null);
            Watchman.Get<HornedAxe>().DeregisterSphere(egressSituation.Storage);
            Object.Destroy(egressSituation.Storage.gameObject);
            EgressSituations.Remove((otherworldId, location.Id));
        }

        private static Otherworld CreateOtherworldFromSpec(CustomOtherworld spec)
        {
            if (_otherworldPrefab == null || _numa == null)
                throw Birdsong.Cack("Trying to create an otherworld before destroying vanilla one");
            var otherworld = Object.Instantiate(_otherworldPrefab, _numa.transform, false);
            otherworld.name = spec.Id;
            otherworld.GetComponent<Image>().SetLocalizedUISprite(Watchman.Get<Config>().GetCurrentCulture().Id,
                spec.Image);
            var coreScript = otherworld.GetComponent<Otherworld>();
            OtherworldIdField(coreScript, spec.Id);
            if (OtherworldDominionsField.Get(coreScript) is not { } dominions)
            {
                dominions = [];
                OtherworldDominionsField.Set(coreScript, dominions);
            }
            Birdsong.TweetQuiet($"Created core {spec.Id}. Creating egresses.");
            foreach (var egress in spec.Egresses ?? Enumerable.Empty<Egress>())
                dominions.Add(CreateOtherworldDominionFromSpec(spec.Id, egress, otherworld.transform));
            if (OtherworldEnRouteField(coreScript) is { } enRouteSphere)
            {
                enRouteSphere.gameObject.name = $"{spec.Id}enroutesphere";
                enRouteSphere.transform.SetAsLastSibling();
                enRouteSphere.SetPropertiesFromSpec(new SphereSpec(enRouteSphere.GetType(), enRouteSphere.name));
                Watchman.Get<HornedAxe>().RegisterSphere(enRouteSphere);
            }
            else
                Birdsong.TweetLoud($"Missing enroute sphere in the otherworld {spec.Id}.");
            
            return coreScript;
        }

        private static OtherworldDominion CreateOtherworldDominionFromSpec(string otherworldId, Egress egress,
            Transform parent)
        {
            if (_egressPrefab == null)
                throw Birdsong.Cack("Trying to create an otherworld before destroying vanilla one");
            var dominion = Object.Instantiate(_egressPrefab, parent, false);
            dominion.name = $"{otherworldId}Dominion{egress.Id}";
            dominion.transform.position = new Vector3(egress.Position.x, egress.Position.y, 0);
            dominion.SetActive(true);
            var coreScript = dominion.GetComponent<OtherworldDominion>();
            OtherworldDominionIdField.Set(coreScript, egress.Id);
            OtherworldDominionAlwaysVisibleField(coreScript, egress.AlwaysVisible.value);
            if (OtherworldDominionSpheresField(coreScript) is not { } locations)
            {
                throw Birdsong.Cack("Failed to get dominion's spheres.");
            }

            coreScript.EgressSphere.gameObject.name = $"{otherworldId}threshold{egress.Id}";
            coreScript.EgressSphere.SetPropertiesFromSpec(new SphereSpec(coreScript.EgressSphere.GetType(),
                coreScript.EgressSphere.gameObject.name));
            coreScript.EgressSphere.doorColor.SetUISprite(egress.Background!, egress.BackgroundColor);
            coreScript.EgressSphere.slotGlow.GetComponent<Image>().SetUISprite(egress.Hover!,
                egress.HoverColor);
            coreScript.EgressSphere.defaultBackgroundColor = egress.BackgroundColor;
            coreScript.EgressSphere.transform.Find("Icon").GetComponent<Image>().SetUISprite(egress.Icon);
            Watchman.Get<HornedAxe>().RegisterSphere(coreScript.EgressSphere);
            locations.Add(coreScript.EgressSphere);
            Birdsong.TweetQuiet($"Created core {egress.Id} in {otherworldId}. Creating locations.");
            if (!EgressLocations.TryGetValue((otherworldId, egress.Id), out var locationList))
            {
                locationList = [];
                EgressLocations.Add((otherworldId, egress.Id), locationList);
            }
            foreach (var location in egress.Locations)
            {
                var locationResult =
                    CreateEgressLocationFromSpec(egress.Id, location, dominion.transform);
                locations.Add(locationResult.output);
                EgressSituations[(otherworldId, location.Id)] = new EgressPseudoSituation(otherworldId,
                    locationResult.storage,
                    locationResult.output, SituationSpheresField);
                
                locationList.Add((locationResult.output, location));
            }

            if (!Egresses.TryGetValue(otherworldId, out var egressList))
            {
                egressList = [];
                Egresses.Add(otherworldId, egressList);
            }
            egressList.Add((coreScript, egress));
            return coreScript;
        }


        // ReSharper disable once UnusedMethodReturnValue.Local
        private static (EgressStorageSphere storage, OutputSphere output) CreateEgressLocationFromSpec(string egressId,
            EgressLocation location, Transform parent)
        {
            if (_locationOutputPrefab == null)
                throw Birdsong.Cack("Trying to create an otherworld before destroying vanilla one");
            var outputSphere = Object.Instantiate(_locationOutputPrefab, parent, false);
            outputSphere.name = $"{egressId}Sphere{location.Id}_output";
            outputSphere.transform.position = new Vector3(location.Position.x, location.Position.y);
            outputSphere.SetActive(true);
            var output = outputSphere.GetComponent<OutputSphere>();
            output.AlwaysShroudIncomingTokens = location.Shrouded.value;
            output.SetPropertiesFromSpec(new SphereSpec(typeof(OutputSphere), $"{location.Id}_output"));
            Watchman.Get<HornedAxe>().RegisterSphere(output);
            Birdsong.TweetQuiet($"Created output for {location.Id} in {egressId}.");
            if (_locationStoragePrefab == null)
                throw Birdsong.Cack("Trying to create an otherworld before destroying vanilla one");
            var storageSphere = Object.Instantiate(_locationStoragePrefab, parent, false);
            storageSphere.name = $"{egressId}Sphere{location.Id}_storage";
            storageSphere.transform.position = new Vector3(location.Position.x, location.Position.y);
            storageSphere.SetActive(true);
            var storage = storageSphere.GetComponent<EgressStorageSphere>();
            storage.SetPropertiesFromSpec(new SphereSpec(typeof(EgressStorageSphere), location.Id));
            Watchman.Get<HornedAxe>().RegisterSphere(storage);
            Birdsong.TweetQuiet($"Created storage for {location.Id} in {egressId}.");
            return (storage, output);
        }
    }
}