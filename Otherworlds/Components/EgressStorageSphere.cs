using SecretHistories.Enums;
using SecretHistories.Spheres;
using SecretHistories.UI;

namespace AviaryModules.Otherworlds.Components;

public class EgressStorageSphere : Sphere
{
    public override SphereCategory SphereCategory => SphereCategory.SituationStorage;
    
    public override bool AllowDrag => false;
    
    public override void AcceptToken(Token token, Context context)
    {
        base.AcceptToken(token, context);
        if (context.actionSource == Context.ActionSource.CalvedStack)
            return;
        TryMergeIncomingWithPresentTokens(token);
    }

    public void TryMergeIncomingWithPresentTokens(Token incomingToken)
    {
        if (incomingToken.Payload.HasUniqueness())
            return;
        for (var index = 0; index < _tokens.Count; ++index)
        {
            if (_tokens[index].Payload.HasUniqueness() || !_tokens[index].CanMergeWithToken(incomingToken)) continue;
            _tokens[index].Payload.SetQuantity(_tokens[index].Quantity + incomingToken.Quantity);
            incomingToken.Retire();
            break;
        }
    }
}