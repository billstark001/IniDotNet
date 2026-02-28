using System;
using System.IO;
using IniDotNet.Integrated;
using IniDotNet.Linq;

namespace IniDotNet.Example
{
    public class MainProgram
    {
        public static void Main()
        {
            var testIniFile = @"#This section provides the general configuration of the application
[GeneralConfiguration] 

#Update rate in msecs
setUpdate = 100

#Maximun errors before quit
setMaxErrors = 2

#Users allowed to access the system
#format: user = pass
[Users]
ricky = rickypass
patty = pattypass ";

            // ── Linq / IniObject API ──────────────────────────────────────────────

            var parser = new IniDataParser();
            // Use '#' as the comment character
            parser.Configuration.AllowNumberSignComments = true;
            parser.Scheme.AssignFrom(new(parser.Configuration));

            IniObject parsedData = parser.Parse(testIniFile);

            Console.WriteLine("---- INI file contents ----\n");
            Console.WriteLine(parsedData);   // IniObject.ToString() uses the formatter
            Console.WriteLine();

            Console.WriteLine("---- setMaxErrors (GeneralConfiguration) ----");
            Console.WriteLine("setMaxErrors = " + parsedData["GeneralConfiguration"]?["setMaxErrors"]);
            Console.WriteLine();

            // Modify and display
            parsedData["GeneralConfiguration"]!["setMaxErrors"] = "10";
            parsedData.Sections.Add("newSection");
            parsedData.Sections.FindByName("newSection")!.Comments
                .Add("This is a new comment for the section");
            parsedData.Sections.FindByName("newSection")!.Properties.Add("myNewKey", "value");
            parsedData.Sections.FindByName("newSection")!.Properties.FindByKey("myNewKey")!.Comments
                .Add("new key comment");

            Console.WriteLine("---- Modified INI file ----");
            Console.WriteLine(parsedData);
            Console.WriteLine();

            // ── Object-model (Integrated) API ────────────────────────────────────

            // Deserialize into a strongly-typed ConfigModel using the generic handler
            var iniParser = new IniParser();
            iniParser.Configuration.AllowNumberSignComments = true;
            iniParser.Scheme.AssignFrom(new(parser.Configuration));

            var handler = new IntegratedIniHandler<ConfigModel>();
            iniParser.Parse(new StringReader(testIniFile), handler);
            ConfigModel result = handler.Data!;

            Console.WriteLine("---- Deserialized ConfigModel ----");
            Console.WriteLine(result);
            Console.WriteLine();

            // Static convenience API: serialize back to INI string
            string serialized = IniParser.Serialize(result);
            Console.WriteLine("---- Re-serialized INI ----");
            Console.WriteLine(serialized);
        }
    }
}
