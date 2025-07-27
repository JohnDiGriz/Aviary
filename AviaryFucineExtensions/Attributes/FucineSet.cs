using System;
using System.Collections;

namespace SecretHistories.Fucine;

[AttributeUsage(validOn: AttributeTargets.Property)]
public class FucineSet : Fucine
{
    public FucineSet()
    {
        DefaultValue = new ArrayList();
    }

    public FucineSet(object defaultValue)
    {
        DefaultValue = defaultValue;
    }

    public FucineSet(params object[] defaultValue)
    {
        DefaultValue = new ArrayList(defaultValue);
    }
    

    public override AbstractImporter CreateImporterInstance()
    {
        return new FucineSetImporter();
    }
}