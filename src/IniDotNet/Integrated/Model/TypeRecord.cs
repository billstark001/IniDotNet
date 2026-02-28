using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IniDotNet.Util;

namespace IniDotNet.Integrated.Model;

public class TypeRecord
{
    #region Fields
    readonly Type _t;

    readonly Dictionary<string, PropertyInfo> _propsKey;
    readonly Dictionary<string, PropertyInfo> _propsSection;

    readonly Dictionary<string, SerializerRecord> _serializerRecords;

    readonly bool _isCtorNoParam;
    readonly int _ctorParamLength;
    readonly Dictionary<string, int> _ctorParamPos;
    readonly ConstructorInfo _ctor;

    #endregion


    public Type Type => _t;

    public bool HasNoParamConstructor => _isCtorNoParam;
    public int ConstructorParamLength => _ctorParamLength;
    public ConstructorInfo Constructor => _ctor;

    public TypeRecord(Type t, bool inherit = true, bool omitIncomplete = true, bool forcePublic = true)
    {
        _t = t;
        _serializerRecords = new();

        if (!ConvertUtil.IsStringDictionary(t))
        {

            (_propsKey, _propsSection) = AttributeUtil.GetTaggedPropertyList(t, inherit);
            var access = AttributeUtil.IsPropertiesFullyAccessible((_propsKey, _propsSection));
            if (!access)
            {
                var res = AttributeUtil.GetTaggedConstructor(t, (_propsKey, _propsSection), omitIncomplete, forcePublic);
                if (res.HasValue)
                    (_ctorParamLength, _ctorParamPos, _ctor) = res.Value;
                else
                    throw new InvalidOperationException("Unsupported type");
            }
            else
            {
                _ctorParamLength = 0;
                _ctorParamPos = new();
                _ctor = AttributeUtil.GetNoParamConstructor(t, forcePublic) ?? throw new InvalidOperationException("Unsupported type");
            }

            _isCtorNoParam = access;
            foreach (var (propName, propInfo) in _propsKey)
            {
                var cvrtType = propInfo.GetCustomAttribute<IniSerializerAttribute>()?.Type;
                SerializerRecord? cvrt = null;
                if (cvrtType != null)
                    cvrt = ConvertUtil.TryGetConverterInnerRefl(propInfo.PropertyType, cvrtType);
                else
                    cvrt = ConvertUtil.TryGetConverterRefl(propInfo.PropertyType, AppDomain.CurrentDomain.GetAssemblies());
                if (cvrt == null && !omitIncomplete)
                    throw new InvalidOperationException("Incompleted type");
                if (cvrt != null)
                    _serializerRecords[propName] = cvrt;

            }
        }
        else
        {
            _propsKey = new();
            _propsSection = new();
            _ctorParamPos = new();
            _ctor = t.GetConstructors()[0];
        }

    }

    public IEnumerable<Type> GetReferenceTypes()
    {
        return _propsSection.Values
            .Select(x => x.PropertyType)
            .Where(x => x != null)
            .Select(x => x!);
    }

    public SerializerRecord? TryGetSerializer(string name)
    {
        return _serializerRecords.TryGetValue(name, out var ret) ? ret : null;
    }

    public PropertyInfo? TryGetProperty(string name)
    {
        var succ = _propsKey.TryGetValue(name, out var ret);
        if (!succ)
            succ = _propsSection.TryGetValue(name, out ret);
        return succ ? ret : null;
    }

    public IReadOnlyDictionary<string, PropertyInfo> KeyProperties => _propsKey;
    public IReadOnlyDictionary<string, PropertyInfo> SectionProperties => _propsSection;
    public bool IsSectionProperty(string x) => _propsSection.ContainsKey(x);
    public int GetConstructorPosition(string x) => _ctorParamPos.TryGetValue(x.ToLower(), out var ret) ? ret : -1;

}
