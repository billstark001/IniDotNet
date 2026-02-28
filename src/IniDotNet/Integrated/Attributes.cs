using System;
using System.Linq;
using IniDotNet.Integrated.Serializer;

namespace IniDotNet.Integrated;


public enum IniType
{
    Auto = 0, 
    Key = 1, 
    Section = 2, 
    // Comment = 4
}

[AttributeUsage(AttributeTargets.Class)]
public class IniModelAttribute : Attribute
{

}


[AttributeUsage(AttributeTargets.Property)]
public class IniPropertyAttribute : Attribute
{
    public string Name { get; set; } = "";
    public IniType Type { get; set; } = IniType.Auto;

    public IniPropertyAttribute()
    {
    }
    public IniPropertyAttribute(string name)
    {
        Name = name;
    }
    public IniPropertyAttribute(string name, IniType type) : this(name)
    {
        Type = type;
    }
}


[AttributeUsage(AttributeTargets.Property)]
public class IniIgnoreAttribute : Attribute
{
    public IniIgnoreAttribute()
    {
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class IniSerializerAttribute : Attribute
{
    public Type Type { get; set; }

    private static readonly Type IniSerializerType = typeof(IIniSerializer<>);

    public IniSerializerAttribute(Type type)
    {
        var implementsInterface = type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == IniSerializerType);
        if (!implementsInterface)
            throw new ArgumentException($"Type {type.Name} must implement IIniSerializer<T>.", nameof(type));
        Type = type;
    }
}
