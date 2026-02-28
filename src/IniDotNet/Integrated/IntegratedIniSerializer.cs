using IniDotNet.Integrated.Model;
using IniDotNet.Linq;
using IniDotNet.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IniDotNet.Integrated;

/// <summary>
/// Serializes a typed model object decorated with <see cref="IniModelAttribute"/> into an
/// <see cref="IniObject"/> that can be formatted as an INI string.
/// </summary>
public class IntegratedIniSerializer
{
    private readonly Dictionary<Type, TypeRecord> _records;

    /// <summary>
    /// Initializes the serializer by pre-computing type metadata for
    /// <paramref name="baseType"/> and all reachable section types.
    /// </summary>
    public IntegratedIniSerializer(Type baseType)
    {
        _records = new Dictionary<Type, TypeRecord>();
        var queue = new Queue<Type>();
        queue.Enqueue(baseType);
        int limit = IntegratedIniHandler.MAX_SEARCH_DEPTH;

        while (queue.Count > 0 && limit-- > 0)
        {
            var type = queue.Dequeue();
            if (_records.ContainsKey(type)) continue;
            _records[type] = new TypeRecord(type, true, true, true);
            foreach (var sub in _records[type].GetReferenceTypes())
                queue.Enqueue(sub);
        }
    }

    /// <summary>Serializes <paramref name="obj"/> into an <see cref="IniObject"/>.</summary>
    public IniObject Serialize(object obj, Type type)
    {
        if (!_records.TryGetValue(type, out var record))
            throw new InvalidOperationException($"Type {type.FullName} was not registered.");

        var iniData = new IniObject();
        WriteKeyProperties(obj, record, iniData.Global);
        WriteSectionProperties(obj, record, iniData);
        return iniData;
    }

    private void WriteKeyProperties(object obj, TypeRecord record, IniPropertyCollection props)
    {
        foreach (var kv in record.KeyProperties)
        {
            var serializer = record.TryGetSerializer(kv.Key);
            if (serializer == null) continue;
            var value = kv.Value.GetValue(obj);
            props.Add(kv.Key, serializer.Serialize(value));
        }
    }

    private void WriteSectionProperties(object obj, TypeRecord record, IniObject iniData)
    {
        foreach (var kv in record.SectionProperties)
        {
            var sectionValue = kv.Value.GetValue(obj);
            if (sectionValue == null) continue;

            iniData.Sections.Add(kv.Key);
            var sectionProps = iniData.Sections.FindByName(kv.Key)!.Properties;
            var sectionType = kv.Value.PropertyType;

            if (ConvertUtil.IsStringDictionary(sectionType))
                WriteDictionarySection(sectionValue, sectionType, sectionProps);
            else if (_records.TryGetValue(sectionType, out var subRecord))
                WriteKeyProperties(sectionValue, subRecord, sectionProps);
        }
    }

    private static void WriteDictionarySection(object dictObj, Type dictType, IniPropertyCollection props)
    {
        if (dictObj is Hashtable ht)
        {
            foreach (DictionaryEntry entry in ht)
                props.Add(entry.Key.ToString()!, entry.Value?.ToString() ?? "");
            return;
        }

        if (dictObj is IDictionary dict)
        {
            SerializerRecord? ser = null;
            var args = dictType.GetGenericArguments();
            if (args.Length == 2)
                ser = ConvertUtil.TryGetConverterRefl(args[1], AppDomain.CurrentDomain.GetAssemblies());

            foreach (DictionaryEntry entry in dict)
            {
                var strValue = ser != null ? ser.Serialize(entry.Value) : entry.Value?.ToString() ?? "";
                props.Add(entry.Key.ToString()!, strValue);
            }
        }
    }
}

/// <summary>
/// Strongly-typed wrapper around <see cref="IntegratedIniSerializer"/> for <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The model type to serialize.</typeparam>
public class IntegratedIniSerializer<T> : IntegratedIniSerializer
{
    /// <summary>Initializes the serializer for <typeparamref name="T"/>.</summary>
    public IntegratedIniSerializer() : base(typeof(T)) { }

    /// <summary>Serializes <paramref name="obj"/> into an <see cref="IniObject"/>.</summary>
    public IniObject Serialize(T obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        return base.Serialize(obj, typeof(T));
    }
}
