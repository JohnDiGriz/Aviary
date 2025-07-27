using System;
using System.Collections;

namespace SecretHistories.Fucine;

[AttributeUsage(AttributeTargets.Property)]
public class FucineBoolExp: Fucine
{
    
    public FucineBoolExp() { DefaultValue = new ArrayList(); }
    public FucineBoolExp(object defaultValue) { DefaultValue = defaultValue; }
    public FucineBoolExp(params object[] defaultValue) { DefaultValue = new ArrayList(defaultValue); }

    public override AbstractImporter CreateImporterInstance() { return new BoolExpImporter(); }
}