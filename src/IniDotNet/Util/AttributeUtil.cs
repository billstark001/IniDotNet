using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using IniDotNet.Integrated;

namespace IniDotNet.Util;

using DSP = Dictionary<string, PropertyInfo>;

public static class AttributeUtil
{
    public static IEnumerable<Type> GetTaggedTypes<T>(Assembly[] assembly) where T : Attribute
    {
        foreach (Type type in assembly.SelectMany(s => s.GetTypes()))
        {
            if (type.GetCustomAttributes(typeof(T), true).Length > 0)
            {
                yield return type;
            }
        }
    }

    public static (DSP, DSP) GetTaggedPropertyList(
        Type type,
        bool inherit = true)
    {
        DSP ansKey = new();
        DSP ansSection = new();

        foreach (var prop in type.GetProperties())
        {
            if (prop.GetCustomAttributes<IniIgnoreAttribute>(inherit).Count() > 0)
                continue;


            var processFlag = false;
            foreach (var a in prop.GetCustomAttributes<IniPropertyAttribute>(inherit))
            {
                // determing the name
                var aName = a.Name; // TODO determine trim or not
                var fieldName = string.IsNullOrEmpty(aName) ? prop.Name : aName;
                processFlag = true;

                var isSection = false;
                if (a.Type == IniType.Section)
                    isSection = true;
                else if (a.Type == IniType.Key)
                    isSection = false;
                else
                {
                    if (ConvertUtil.IsStringDictionary(prop.PropertyType))
                        isSection = true;
                    else if (prop.PropertyType.GetCustomAttribute<IniModelAttribute>() != null)
                        isSection = true;
                    // TODO add support of ini data
                }

                // write record
                if (isSection)
                    ansSection[fieldName] = prop;
                else
                    ansKey[fieldName] = prop;
            }

            if (!processFlag)
            {
                var isSection = false;
                if (ConvertUtil.IsStringDictionary(prop.PropertyType))
                    isSection = true;
                else if (prop.PropertyType.GetCustomAttribute<IniModelAttribute>() != null)
                    isSection = true;
                if (isSection)
                    ansSection[prop.Name] = prop;
                else
                    ansKey[prop.Name] = prop;
            }

        }
        return (ansKey, ansSection);
    }

    public static bool IsPropertiesFullyAccessible((DSP, DSP) record)
    {
        foreach (var item in record.Item1.Values.Concat(record.Item2.Values))
            if (!item.CanWrite || !(item.GetSetMethod()?.IsPublic ?? true))
                return false;
        return true;
    }

    public static ConstructorInfo? GetNoParamConstructor(
        Type type,
        bool forcePublic = true)
    {
        foreach (var ctor in type.GetConstructors().OrderByDescending(x => x.IsPublic))
            if (ctor.GetParameters().Length == 0)
                if (!forcePublic || ctor.IsPublic)
                    return ctor;
        return null;
    }

    public static (int, Dictionary<string, int>, ConstructorInfo)? GetTaggedConstructor(
        Type type,
        (DSP, DSP)? properties = null,
        bool omitIncompleteConstructors = true,
        bool forcePublic = true)
    {
        properties = properties ?? GetTaggedPropertyList(type);
        var (propKey, propSection) = properties.Value!;

        // [prop name] = applicable ini field names
        Dictionary<string, HashSet<string>> inverseProps = new();
        foreach (var (k, v) in propKey.Concat(propSection))
        {
            var vname = v.Name.ToLower();
            if (!inverseProps.ContainsKey(vname))
                inverseProps[vname] = new();
            inverseProps[vname].Add(k.ToLower());
        }

        HashSet<string> props = new(inverseProps.Keys.Select(x => x.ToLower()));
        HashSet<string> iniProps = new(propKey.Keys.Concat(propSection.Keys));

        foreach (var ctor in type.GetConstructors().OrderBy(x => -x.GetParameters().Length))
        {
            var prms = ctor.GetParameters();
            // check incomplete ctors
            if (prms.Length != props.Count && omitIncompleteConstructors)
                continue;

            // [iniparam] = ctor param pos
            Dictionary<string, int> paramMap = new();
            for (int i = 0; i < prms.Length; ++i)
            {
                var pnameOrig = prms[i].Name;
                if (pnameOrig == null)
                    continue;

                var pname = pnameOrig.ToLower();
                var flp = props.Contains(pname);
                var fli = iniProps.Contains(pname.ToLower());
                if (flp)
                {
                    paramMap[pname] = i;
                    foreach (var other in inverseProps[pname])
                        paramMap[other] = i;
                }
                else if (fli)
                {
                    string? origName =
                        propKey.TryGetValue(pname, out var prop) ?
                        prop.Name.ToLower() :
                        propSection[pname].Name.ToLower();
                    paramMap[origName] = i;
                    foreach (var other in inverseProps[origName])
                        paramMap[other] = i;
                }
                else if (omitIncompleteConstructors)
                    continue;

            }

            // if no continue is triggered, this constructor is available
            if (!forcePublic || ctor.IsPublic)
                return (prms.Length, paramMap, ctor);

        }

        return null;
    }
}
