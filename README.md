# IniDotNet

A .NET library for reading and writing INI files, featuring both a flexible **Linq-style** object model and a **System.Text.Json–inspired** attribute-based serialization API.

## Target Frameworks

- `net8.0` (LTS)
- `netstandard2.1`

## Installation

```powershell
dotnet add package ini-dotnet
```

---

## Quick Start

### Parse into a raw `IniObject`

```csharp
using IniDotNet;
using IniDotNet.Linq;

var parser = new IniDataParser();
IniObject data = parser.Parse(File.ReadAllText("config.ini"));

// Read
string host = data["Server"]["host"];

// Write
data["Server"]["host"] = "newhost";

// Format back to string
Console.WriteLine(data);          // uses IniObject.ToString()
```

---

## Object-Model API

Define a model class and decorate it with attributes, similar to `System.Text.Json`.

### Attributes

| Attribute | Target | Description |
|---|---|---|
| `[IniModel]` | class | Marks a class as a serializable INI section type. |
| `[IniProperty("name")]` | property | Maps a property to a specific INI key or section name. Omit to use the property name. |
| `[IniProperty("name", IniType.Section)]` | property | Forces a property to be treated as a section. |
| `[IniProperty("name", IniType.Key)]` | property | Forces a property to be treated as a key. |
| `[IniIgnore]` | property | Excludes a property from serialization/deserialization. |
| `[IniSerializer(typeof(MySerializer))]` | property | Uses a custom `IIniSerializer<T>` for this property. |

### Supported property types (built-in serializers)

`string`, `bool`, `int`, `long`, `double`, `string[]`, `int[]`, `IEnumerable<string>`,
`Dictionary<string, T>` (mapped as a section), `Hashtable` (mapped as a section).

### Define a model

```csharp
using IniDotNet.Integrated;

[IniModel]
public class ServerConfig
{
    [IniProperty("host")]
    public string Host { get; set; } = "";

    [IniProperty("port")]
    public int Port { get; set; }

    [IniProperty("enabled")]
    public bool Enabled { get; set; }
}

public class AppConfig
{
    [IniProperty("version")]
    public string Version { get; set; } = "";

    // Recognized as a section because ServerConfig has [IniModel]
    public ServerConfig Server { get; set; } = new();

    // Dictionary<string,string> is always a section
    public Dictionary<string, string> Users { get; set; } = new();
}
```

### Deserialize

```csharp
// Static convenience method
AppConfig cfg = IniParser.Deserialize<AppConfig>(iniString);

// Or via IniDataParser
var parser = new IniDataParser();
AppConfig cfg = parser.ParseAs<AppConfig>(iniString);

// Or via the handler directly (allows parser customization)
var handler = new IntegratedIniHandler<AppConfig>();
new IniParser().Parse(new StringReader(iniString), handler);
AppConfig cfg = handler.Data!;
```

### Serialize

```csharp
// Static convenience method → INI string
string iniString = IniParser.Serialize(cfg);

// Or produce an IniObject for further manipulation
var serializer = new IntegratedIniSerializer<AppConfig>();
IniObject iniData = serializer.Serialize(cfg);
string iniString = iniData.ToString();
```

### Round-trip example

```csharp
var ini = """
[Server]
host = localhost
port = 8080
enabled = true

[Users]
alice = pass1
bob   = pass2
""";

var cfg = IniParser.Deserialize<AppConfig>(ini);

cfg.Server.Port = 9090;
cfg.Users["carol"] = "pass3";

string updated = IniParser.Serialize(cfg);
Console.WriteLine(updated);
```

---

## Custom serializer

Implement `IIniSerializer<T>` for any type not supported out of the box:

```csharp
public class ColorSerializer : IIniSerializer<Color>
{
    public string Serialize(Color? value) => value?.ToHex() ?? "";
    public Color? Deserialize(string? s) => s == null ? null : Color.FromHex(s);
}

public class ThemeConfig
{
    [IniSerializer(typeof(ColorSerializer))]
    public Color Background { get; set; }
}
```

---

## Parser configuration

```csharp
var parser = new IniDataParser();

parser.Configuration.AllowNumberSignComments = true;   // '#' as comment char
parser.Configuration.CaseInsensitive         = true;   // case-insensitive keys
parser.Configuration.AllowMultilineProperties = true;  // backslash line continuation
parser.Configuration.UseEscapeCharacters     = true;   // \n \t \uXXXX in values

parser.Scheme.AssignFrom(new IniScheme(parser.Configuration));
```

## Merging

```csharp
IniObject defaults = parser.Parse(File.ReadAllText("defaults.ini"));
IniObject user     = parser.Parse(File.ReadAllText("user.ini"));
defaults.Merge(user);   // user values overwrite defaults
```

---

## Project structure

```
src/
  IniDotNet/
    Base/           – interfaces and configuration types
    Linq/           – IniObject / IniSection / IniProperty DOM
    Integrated/     – attribute-based serialization/deserialization
      Serializer/   – built-in IIniSerializer<T> implementations
      Handler/      – section handlers used during parsing
      Model/        – TypeRecord / SerializerRecord metadata
    Util/           – reflection helpers
  IniDotNet.Tests/  – NUnit test suite
  IniDotNet.Example – console example
```

## License

MIT
