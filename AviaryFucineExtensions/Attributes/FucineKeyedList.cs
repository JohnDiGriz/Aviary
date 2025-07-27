using System;
using System.Collections;
using System.Globalization;

namespace SecretHistories.Fucine;

[AttributeUsage(validOn: AttributeTargets.Property)]
public class FucineKeyedList : Fucine
{
    public FucineKeyedList()
    {
        DefaultValue = new ArrayList();
    }

    public FucineKeyedList(object defaultValue)
    {
        DefaultValue = defaultValue;
    }

    public FucineKeyedList(params object[] defaultValue)
    {
        DefaultValue = new ArrayList(defaultValue);
    }

    public string? PropertyToInject { get; set; }
    
    public string? QuickSpecProperty { get; set; }
    

    public override AbstractImporter CreateImporterInstance()
    {
        var importer = new KeyedListImporter()
        {
            QuickSpecProperty = QuickSpecProperty
        };
        if(PropertyToInject is not null)
            importer.PropertyToInject = PropertyToInject;
        return importer;
    }
}