using System;
using System.Collections.Generic;
using Roost;
using SecretHistories.Abstract;
using SecretHistories.Commands;
using SecretHistories.Entities;
using SecretHistories.Infrastructure;
using SecretHistories.UI;

namespace AviaryModules.ImprovedFX;

public static class ImprovedFXMaster
{
    private const string FX_ESSENTIAL = "fxEssential";

    private const string FX_REQUIRED = "fxRequired";

    private const string FX_FORBIDDEN = "fxForbidden";

    internal static void Enact()
    {
        Machine.ClaimProperty<Recipe, Dictionary<string, string>>(FX_ESSENTIAL);

        Machine.ClaimProperty<Recipe, Dictionary<string, string>>(FX_REQUIRED);

        Machine.ClaimProperty<Recipe, Dictionary<string, string>>(FX_FORBIDDEN);

        AtTimeOfPower.NewGame.Schedule(SetLegacyFx, PatchType.Postfix);

        AtTimeOfPower.RecipeRequirementsCheck.Schedule(CheckRecipeFX, PatchType.Postfix);
    }

    public static void SetLegacyFx(MenuScreenController __instance, string legacyId)
    {
        new EnviroFxCommand("legacy", legacyId).ExecuteOn((ITokenPayload)null!);
    }

    public static void CheckRecipeFX(Recipe __instance, ref bool __result)
    {
        if (!__result)
            return;
        var fxEssential = __instance.RetrieveProperty<Dictionary<string, string>?>(FX_ESSENTIAL);
        if (fxEssential is not null && fxEssential.Count != 0 && !fxEssential.FXEssentialSatisfied())
        {
            __result = false;
            return;
        }

        var fxRequired = __instance.RetrieveProperty<Dictionary<string, string>?>(FX_REQUIRED);
        if (fxRequired is not null && fxRequired.Count != 0 && fxRequired.FXRequiredSatisfied())
        {
            __result = false;
            return;
        }

        var fxForbidden = __instance.RetrieveProperty<Dictionary<string, string>?>(FX_FORBIDDEN);
        if(fxForbidden is not null && fxForbidden.Count != 0 && fxForbidden.FXForbiddenSatisfied())
            __result = false;
    }

    public static bool FXEssentialSatisfied(this Dictionary<string, string> fxReqs)
    {
        var enviroFxCommands = Watchman.Get<Xamanek>().CurrentEnviroFxCommands;
        foreach (var fxReq in fxReqs)
        {
            if (!enviroFxCommands.TryGetValue(fxReq.Key, out var fx) || !string.Equals(fx.Effect, fxReq.Value,
                    StringComparison.InvariantCultureIgnoreCase))
                return false;
        }

        return true;
    }

    public static bool FXRequiredSatisfied(this Dictionary<string, string> fxReqs)
    {
        var enviroFxCommands = Watchman.Get<Xamanek>().CurrentEnviroFxCommands;
        foreach (var fxReq in fxReqs)
        {
            if (enviroFxCommands.TryGetValue(fxReq.Key, out var fx) && string.Equals(fx.Effect, fxReq.Value,
                    StringComparison.InvariantCultureIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool FXForbiddenSatisfied(this Dictionary<string, string> fxReqs)
    {
        var enviroFxCommands = Watchman.Get<Xamanek>().CurrentEnviroFxCommands;
        foreach (var fxReq in fxReqs)
        {
            if (enviroFxCommands.TryGetValue(fxReq.Key, out var fx) && string.Equals(fx.Effect, fxReq.Value,
                    StringComparison.InvariantCultureIgnoreCase))
                return false;
        }

        return true;
    }
}