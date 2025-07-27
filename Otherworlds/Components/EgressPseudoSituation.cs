using System;
using System.Collections.Generic;
using SecretHistories.Entities;
using SecretHistories.Entities.NullEntities;
using SecretHistories.Spheres;
using SecretHistories.States;
using SecretHistories.UI;

namespace AviaryModules.Otherworlds.Components;

public class EgressPseudoSituation : Situation
{
    
    
    public EgressPseudoSituation(string otherworldId, EgressStorageSphere storage, OutputSphere output, Func<Situation, HashSet<Sphere>> spheresAccessor) : base(NullVerb.Create(), otherworldId)
    {
        Storage = storage;
        Output = output;
        var spheres = spheresAccessor(this);
        spheres.Add(Storage);
        spheres.Add(Output);
        Storage.SetContainer(this);
        Watchman.Get<HornedAxe>().DeregisterSituation(this);
        State = new RequiresExecutionState();
    }

    public EgressStorageSphere? Storage { get; }

    public OutputSphere? Output { get; }
}