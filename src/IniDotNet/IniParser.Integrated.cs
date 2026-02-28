using IniDotNet.Base;
using IniDotNet.Integrated;
using IniDotNet.Linq;
using System;
using System.IO;

namespace IniDotNet;

public partial class IniParser
{
    /// <summary>
    ///     Parses an INI string into a typed model object using default parser settings.
    /// </summary>
    /// <typeparam name="T">
    ///     The model type to deserialize into. Properties are mapped via
    ///     <see cref="IniDotNet.Integrated.IniPropertyAttribute"/> and related attributes.
    /// </typeparam>
    /// <param name="iniString">INI-formatted string.</param>
    /// <returns>A populated <typeparamref name="T"/> instance, or <c>null</c> on failure.</returns>
    public static T? Deserialize<T>(string iniString)
    {
        var parser = new IniParser();
        var handler = new IntegratedIniHandler<T>();
        parser.Parse(new StringReader(iniString), handler);
        return handler.Data;
    }

    /// <summary>
    ///     Serializes a typed model object to an INI-formatted string using default formatting settings.
    /// </summary>
    /// <typeparam name="T">The model type to serialize.</typeparam>
    /// <param name="obj">The object to serialize. Must not be null.</param>
    /// <returns>An INI-formatted string representing <paramref name="obj"/>.</returns>
    public static string Serialize<T>(T obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        var serializer = new IntegratedIniSerializer<T>();
        var iniData = serializer.Serialize(obj);
        return iniData.ToString();
    }
}
