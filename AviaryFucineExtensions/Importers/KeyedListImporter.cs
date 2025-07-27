using System;
using System.Collections;
using System.Globalization;
using SecretHistories.Fucine.DataImport;

namespace SecretHistories.Fucine;

public class KeyedListImporter : AbstractImporter
{
    public string PropertyToInject
    {
        get => string.IsNullOrEmpty(_propertyToInject) ? "id" : _propertyToInject;
        set => _propertyToInject = value.ToLower(CultureInfo.InvariantCulture);
    }
    
    public string? QuickSpecProperty 
    {
        get => _quickSpecProperty;
        set => _quickSpecProperty = value?.ToLower(CultureInfo.InvariantCulture);
    }

    private string? _quickSpecProperty;
    private string? _propertyToInject;

    public override object Import(object importData, Type propertyType)
    {
        if (importData is EntityData keyedList)
        {
            var newArrayList = new ArrayList();
            foreach (var id in keyedList.ValuesTable.Keys)
            {
                if (keyedList.ValuesTable[id] is not EntityData entity)
                {
                    if (string.IsNullOrEmpty(QuickSpecProperty))
                        throw new ApplicationException(
                            $"MALFORMED KEYED LIST - ENTRY '{id}' IS NOT A DICTIONARY BUT NO QUICK SPEC PROPERTY WAS SPECIFIED");
                    entity = new EntityData()
                    {
                        [QuickSpecProperty] = keyedList.ValuesTable[id]
                    };
                }

                entity[PropertyToInject] = id;
                newArrayList.Add(entity);
            }
            importData = newArrayList;
        }
        if(importData is ArrayList)
            return ImportMethods.ImportListWithDefaultEntryImporter(importData, propertyType);
        throw new ApplicationException("MALFORMED KEYED LIST - KEYED LIST IS NEITHER A DICTIONARY NOR A LIST");
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