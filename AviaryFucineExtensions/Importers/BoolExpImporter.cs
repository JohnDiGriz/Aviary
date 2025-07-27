using System;

namespace SecretHistories.Fucine;

public class BoolExpImporter()
    : AbstractImporter
{
    public override object Import(object importData, Type type)
    {
        object result;
        try
        {
            if(importData is bool importBool)
                importData = importBool ? "true"  : "false";
            result = ImportMethods.ImportWithConstructor(importData, type);
        }
        catch (Exception ex)
        {
            throw ex;
        }
        return result;
    }
    public override object GetDefaultValue<T>(CachedFucineProperty<T> cachedFucineProperty)
    {
        if (cachedFucineProperty.FucineAttribute.DefaultValue != null)
            return ImportMethods.ImportWithConstructor(cachedFucineProperty.FucineAttribute.DefaultValue, cachedFucineProperty.ThisPropInfo.PropertyType);

        return FactoryInstantiator.CreateObjectWithDefaultConstructor(cachedFucineProperty.ThisPropInfo.PropertyType);
    }
}