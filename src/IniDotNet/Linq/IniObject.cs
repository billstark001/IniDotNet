using IniDotNet.Base;

namespace IniDotNet.Linq
{
    /// <summary>
    ///     Represents all data from an INI file
    /// </summary>
    public class IniObject : IDeepCloneable<IniObject>
    {
        #region Initialization

        /// <summary>
        ///     Initializes an empty IniObject instance.
        /// </summary>
        public IniObject()
        {
            Global = new IniPropertyCollection();
            Sections = new IniSectionCollection();
            _scheme = new IniScheme();
        }

        /// <summary>
        ///     Initialzes an IniObject instance with a given scheme
        /// </summary>
        /// <param name="scheme"></param>
        public IniObject(IniScheme scheme)
         : this()
        {
            _scheme = scheme.DeepClone();
        }

        public IniObject(IniObject ori)
        {
            Sections = ori.Sections.DeepClone();
            Global = ori.Global.DeepClone();
            Configuration = ori.Configuration.DeepClone();
        }
        #endregion

        /// <summary>
        ///     If set to true, it will automatically create a section when you use the indexed 
        ///     access with a section name that does not exis.
        ///     If set to false, it will throw an exception if you try to access a section that 
        ///     does not exist with the index operator.
        /// </summary>
        /// <remarks>
        ///     Defaults to false.
        /// </remarks>
        public bool CreateSectionsIfTheyDontExist { get; set; } = false;

        /// <summary>
        ///     Configuration used to write an ini file with the proper
        ///     delimiter characters and data.
        /// </summary>
        /// <remarks>
        ///     If the <see cref="IniObject"/> instance was created by a parser,
        ///     this instance is a copy of the <see cref="IniParserConfig"/> used
        ///     by the parser (i.e. different objects instances)
        ///     If this instance is created programatically without using a parser, this
        ///     property returns an instance of <see cref=" IniParserConfig"/>
        /// </remarks>
        public IniParserConfig Configuration
        {
            get
            {
                // Lazy initialization
                if (_configuration == null)
                {
                    _configuration = new IniParserConfig();
                }

                return _configuration;
            }

            set
            {
                _configuration = value.DeepClone();
            }
        }

        public IniScheme Scheme
        {
            get
            {
                // Lazy initialization
                if (_scheme == null)
                {
                    _scheme = new IniScheme();
                }

                return _scheme;
            }

            set
            {
                _scheme = value.DeepClone();
            }
        }

        /// <summary>
        /// 	Global sections. Contains properties which are not
        /// 	enclosed in any section (i.e. they are defined at the beginning 
        /// 	of the file, before any section.
        /// </summary>
        public IniPropertyCollection Global { get; protected set; }

        /// <summary>
        /// Gets the <see cref="IniPropertyCollection"/> instance 
        /// with the specified section name.
        /// with the specified section name.
        /// </summary>
        public IniPropertyCollection? this[string sectionName]
        {
            get
            {
                if (!Sections.Contains(sectionName))
                    if (CreateSectionsIfTheyDontExist)
                        Sections.Add(sectionName);
                    else
                        return null;

                return Sections[sectionName];
            }
        }

        /// <summary>
        /// Gets or sets all the <see cref="IniSection"/> 
        /// for this IniObject instance.
        /// </summary>
        public IniSectionCollection Sections { get; set; }

        /// <summary>
        ///     Deletes all data
        /// </summary>
        public void Clear()
        {
            Global.Clear();
            Sections.Clear();
        }

        /// <summary>
        ///     Deletes all comments in all sections and properties values
        /// </summary>
        public void ClearAllComments()
        {
            Global.ClearComments();
            foreach (var section in Sections)
            {
                section.ClearComments();
                section.Properties.ClearComments();
            }
        }

        /// <summary>
        ///     Merges the other iniData into this one by overwriting existing values.
        ///     Comments get appended.
        /// </summary>
        /// <param name="toMergeIniData">
        ///     IniObject instance to merge into this. 
        ///     If it is null this operation does nothing.
        /// </param>
        public void Merge(IniObject toMergeIniData)
        {
            if (toMergeIniData == null) return;

            Global.Merge(toMergeIniData.Global);
            Sections.Merge(toMergeIniData.Sections);
        }

        #region IDeepCloneable<T> Members

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public IniObject DeepClone()
        {
            return new IniObject(this);
        }

        #endregion

        /// <summary>
        ///     Returns the INI-formatted string representation of this data,
        ///     using default formatting settings.
        /// </summary>
        public override string ToString()
        {
            var formatter = new IniObjectFormatter();
            return formatter.Format(this, new Base.IniFormatterConfig());
        }

        #region Fields
        /// <summary>
        ///     See property <see cref="Configuration"/> for more information. 
        /// </summary>
        private IniParserConfig? _configuration;
        /// <summary>
        ///     Represents all sections from an INI file
        /// </summary>
        protected IniScheme? _scheme;

        #endregion
    }
}