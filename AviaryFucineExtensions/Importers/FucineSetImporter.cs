using System;
using AviaryModules.AviaryFucineExtensions;

namespace SecretHistories.Fucine;

public class FucineSetImporter : AbstractImporter
{
    
    public override object Import(object importData, Type type)
    {
        try
        {
            return AviaryImportMethods.ImportSetWithDefaultEntryImporter(importData, type);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public override object GetDefaultValue<T>(CachedFucineProperty<T> cachedFucineProperty)
    {
        try
        {
            return FactoryInstantiator.CreateObjectWithDefaultConstructor(cachedFucineProperty.ThisPropInfo.PropertyType);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
}