using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Roost;
using SecretHistories.Fucine;

namespace AviaryModules.AviaryFucineExtensions;

public static class AviaryImportMethods
{

    public static void Enact()
    {
        Machine.Patch(
            original: typeof(ImportMethods).GetMethodInvariant(nameof(ImportMethods.GetDefaultImportFuncForType)),
            prefix: typeof(AviaryImportMethods).GetMethodInvariant(nameof(GetDefaultSetImportFunc)));
    }

    private static bool GetDefaultSetImportFunc(Type type, ref ImportMethods.ImportFunc __result)
    {
        if (!type.IsSet()) return true;
        __result = ImportSetWithDefaultEntryImporter;
        return false;

    }
    
    public static object ImportSetWithDefaultEntryImporter(object setData, Type listType)
    {
        var importFuncForType = ImportMethods.GetDefaultImportFuncForType(ImportMethods.GetGenericType(listType).GetGenericArguments()[0]);
        try
        {
            return AviaryImportMethods.ImportSet(setData, listType, importFuncForType);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    
    public static object ImportSet(
        object setData,
        Type setType,
        ImportMethods.ImportFunc importEntry)
    {
        var genericArgument = ImportMethods.GetGenericType(setType).GetGenericArguments()[0];
        if (FactoryInstantiator.CreateObjectWithDefaultConstructor(setType) is not ICollection<object> set || !set.GetType().IsSet())
            throw new ApplicationException("SET IS MALFORMED - COUND NOT CREATE A SET INSTANCE");
        try
        {
            if (setData is not ArrayList arrayList)
            {
                var obj = importEntry(setData, genericArgument);
                set.Add(obj);
            }
            else
            {
                foreach (var valueData in arrayList)
                {
                    var obj = importEntry(valueData, genericArgument);
                    set.Add(obj);
                }
            }
            return set;
        }
        catch (Exception ex)
        {
            throw new ApplicationException("LIST[] IS MALFORMED - " + ex.FormatException());
        }
    }
    
    public static bool IsSet(this Type type)
    {
        var setType = typeof(ISet<>);
        return type.GetInterfaces().Any(i => i is not null && i.IsGenericType && i.GetGenericTypeDefinition() == setType);
    }
}