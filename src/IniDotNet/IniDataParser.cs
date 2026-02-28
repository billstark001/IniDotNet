using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using IniDotNet.Base;
using IniDotNet.Integrated;
using IniDotNet.Linq;

namespace IniDotNet
{
    /// <summary>
	/// 	Responsible for parsing an string from an ini file, and creating
	/// 	an <see cref="IniObject"/> structure.
	/// </summary>
    public partial class IniDataParser : IniParser
    {
        #region Initialization
        /// <summary>
        ///     Ctor
        /// </summary>
        public IniDataParser() : base()
        {
        }

        #endregion


        /// <summary>
        ///     Parses a string containing valid ini data
        /// </summary>
        /// <param name="iniString">
        ///     String with data in INI format
        /// </param>
        public IniObject Parse(string iniString)
        {
            return Parse(new StringReader(iniString));
        }

        /// <summary>
        ///     Parses a string containing valid ini data
        /// </summary>
        /// <param name="textReader">
        ///     Text reader for the source string contaninig the ini data
        /// </param>
        /// <returns>
        ///     An <see cref="IniObject"/> instance containing the data readed
        ///     from the source
        /// </returns>
        /// <exception cref="ParsingException">
        ///     Thrown if the data could not be parsed
        /// </exception>
        public IniObject Parse(TextReader textReader)
        {
            IniObjectHandler iniData = new();
            iniData.Configuration = Configuration;

            Parse(textReader, iniData);

            return iniData.Get();
        }

        /// <summary>
        ///     Parses an INI string directly into a typed model object.
        /// </summary>
        /// <typeparam name="T">The model type to deserialize into.</typeparam>
        /// <param name="iniString">String with data in INI format.</param>
        /// <returns>A <typeparamref name="T"/> instance populated from the INI data.</returns>
        public T? ParseAs<T>(string iniString)
        {
            return ParseAs<T>(new StringReader(iniString));
        }

        /// <summary>
        ///     Parses INI data from a <see cref="TextReader"/> directly into a typed model object.
        /// </summary>
        /// <typeparam name="T">The model type to deserialize into.</typeparam>
        /// <param name="textReader">Text reader for the source INI data.</param>
        /// <returns>A <typeparamref name="T"/> instance populated from the INI data.</returns>
        public T? ParseAs<T>(TextReader textReader)
        {
            var handler = new IntegratedIniHandler<T>();
            Parse(textReader, handler);
            return handler.Data;
        }

    }
}
