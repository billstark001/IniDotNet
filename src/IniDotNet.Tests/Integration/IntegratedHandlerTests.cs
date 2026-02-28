using IniDotNet.Integrated;
using IniDotNet.Integrated.Serializer;
using IniDotNet.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace IniDotNet.Tests.Integration
{
    [TestFixture]
    public class IntegratedHandlerTests
    {
        // ─── Model definitions ────────────────────────────────────────────────────

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

            public ServerConfig Server { get; set; } = new();

            public Dictionary<string, string> Users { get; set; } = new();
        }

        [IniModel]
        public class LogConfig
        {
            [IniProperty("level")]
            public string Level { get; set; } = "";

            [IniProperty("tags")]
            public string[] Tags { get; set; } = System.Array.Empty<string>();
        }

        public class AppConfigWithLog
        {
            public LogConfig Logging { get; set; } = new();
        }

        // Named property (attribute name differs from property name)
        public class AliasConfig
        {
            [IniProperty("max_errors")]
            public int MaxErrors { get; set; }

            [IniProperty("update_ms")]
            public int UpdateMs { get; set; }
        }

        // ─── Deserialization tests ────────────────────────────────────────────────

        [Test]
        public void deserialize_simple_section_into_class()
        {
            const string ini = @"
[Server]
host = localhost
port = 8080
enabled = true
";
            var handler = new IntegratedIniHandler<AppConfig>();
            new IniParser().Parse(new StringReader(ini), handler);
            var cfg = handler.Data;

            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg!.Server.Host, Is.EqualTo("localhost"));
            Assert.That(cfg.Server.Port, Is.EqualTo(8080));
            Assert.That(cfg.Server.Enabled, Is.True);
        }

        [Test]
        public void deserialize_dictionary_section()
        {
            const string ini = @"
[Users]
alice = pass1
bob   = pass2
";
            var handler = new IntegratedIniHandler<AppConfig>();
            new IniParser().Parse(new StringReader(ini), handler);
            var cfg = handler.Data;

            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg!.Users["alice"], Is.EqualTo("pass1"));
            Assert.That(cfg.Users["bob"], Is.EqualTo("pass2"));
        }

        [Test]
        public void deserialize_global_key_into_class()
        {
            const string ini = @"version = 3.0
[Server]
host = example.com
port = 443
enabled = false
";
            var handler = new IntegratedIniHandler<AppConfig>(allowGlobal: true);
            new IniParser().Parse(new StringReader(ini), handler);
            var cfg = handler.Data;

            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg!.Version, Is.EqualTo("3.0"));
            Assert.That(cfg.Server.Host, Is.EqualTo("example.com"));
        }

        [Test]
        public void deserialize_string_array_property()
        {
            const string ini = @"
[Logging]
level = debug
tags = web,api,db
";
            var handler = new IntegratedIniHandler<AppConfigWithLog>();
            new IniParser().Parse(new StringReader(ini), handler);
            var cfg = handler.Data;

            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg!.Logging.Level, Is.EqualTo("debug"));
            Assert.That(cfg.Logging.Tags, Is.EqualTo(new[] { "web", "api", "db" }));
        }

        [Test]
        public void deserialize_with_attribute_name_alias()
        {
            const string ini = @"max_errors = 5
update_ms  = 200
";
            var handler = new IntegratedIniHandler<AliasConfig>(allowGlobal: true);
            new IniParser().Parse(new StringReader(ini), handler);
            var cfg = handler.Data;

            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg!.MaxErrors, Is.EqualTo(5));
            Assert.That(cfg.UpdateMs, Is.EqualTo(200));
        }

        [Test]
        public void generic_handler_typed_data_property_returns_correct_type()
        {
            const string ini = @"
[Server]
host = myhost
port = 9000
enabled = true
";
            var handler = new IntegratedIniHandler<AppConfig>();
            new IniParser().Parse(new StringReader(ini), handler);

            Assert.That(handler.Data, Is.InstanceOf<AppConfig>());
            Assert.That(handler.Data!.Server.Port, Is.EqualTo(9000));
        }

        // ─── Serialization tests ─────────────────────────────────────────────────

        [Test]
        public void serialize_class_section_produces_correct_ini()
        {
            var cfg = new AppConfig
            {
                Server = new ServerConfig { Host = "prod.example.com", Port = 443, Enabled = true }
            };

            var serializer = new IntegratedIniSerializer<AppConfig>();
            var iniData = serializer.Serialize(cfg);

            Assert.That(iniData.Sections.Contains("Server"), Is.True);
            Assert.That(iniData["Server"]!["host"], Is.EqualTo("prod.example.com"));
            Assert.That(iniData["Server"]!["port"], Is.EqualTo("443"));
            Assert.That(iniData["Server"]!["enabled"], Is.EqualTo("true"));
        }

        [Test]
        public void serialize_dictionary_section_produces_correct_ini()
        {
            var cfg = new AppConfig
            {
                Users = new Dictionary<string, string> { ["alice"] = "pass1", ["bob"] = "pass2" }
            };

            var serializer = new IntegratedIniSerializer<AppConfig>();
            var iniData = serializer.Serialize(cfg);

            Assert.That(iniData.Sections.Contains("Users"), Is.True);
            Assert.That(iniData["Users"]!["alice"], Is.EqualTo("pass1"));
            Assert.That(iniData["Users"]!["bob"], Is.EqualTo("pass2"));
        }

        [Test]
        public void serialize_global_key_produces_correct_ini()
        {
            var cfg = new AppConfig { Version = "2.5" };
            var serializer = new IntegratedIniSerializer<AppConfig>();
            var iniData = serializer.Serialize(cfg);

            Assert.That(iniData.Global["version"], Is.EqualTo("2.5"));
        }

        // ─── Round-trip tests ─────────────────────────────────────────────────────

        [Test]
        public void roundtrip_serialize_then_deserialize()
        {
            var original = new AppConfig
            {
                Version = "1.0",
                Server = new ServerConfig { Host = "localhost", Port = 5000, Enabled = true },
                Users = new Dictionary<string, string> { ["admin"] = "secret" }
            };

            var serializer = new IntegratedIniSerializer<AppConfig>();
            var iniData = serializer.Serialize(original);
            var iniString = iniData.ToString();

            var parser = new IniParser();
            var handler = new IntegratedIniHandler<AppConfig>(allowGlobal: true);
            parser.Parse(new StringReader(iniString), handler);
            var restored = handler.Data;

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.Version, Is.EqualTo("1.0"));
            Assert.That(restored.Server.Host, Is.EqualTo("localhost"));
            Assert.That(restored.Server.Port, Is.EqualTo(5000));
            Assert.That(restored.Server.Enabled, Is.True);
            Assert.That(restored.Users["admin"], Is.EqualTo("secret"));
        }

        // ─── Static convenience API tests ────────────────────────────────────────

        [Test]
        public void static_deserialize_works()
        {
            const string ini = @"
[Server]
host = api.example.com
port = 80
enabled = false
";
            var cfg = IniParser.Deserialize<AppConfig>(ini);

            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg!.Server.Host, Is.EqualTo("api.example.com"));
            Assert.That(cfg.Server.Port, Is.EqualTo(80));
            Assert.That(cfg.Server.Enabled, Is.False);
        }

        [Test]
        public void static_serialize_produces_parseable_output()
        {
            var cfg = new AppConfig
            {
                Server = new ServerConfig { Host = "test", Port = 1234, Enabled = true }
            };

            var iniString = IniParser.Serialize(cfg);

            Assert.That(iniString, Does.Contain("[Server]"));
            Assert.That(iniString, Does.Contain("host = test"));
            Assert.That(iniString, Does.Contain("port = 1234"));
        }

        // ─── IniObject.ToString tests ─────────────────────────────────────────────

        [Test]
        public void ini_object_tostring_produces_valid_ini()
        {
            var parser = new IniDataParser();
            const string ini = "[section]\nkey = value";
            var obj = parser.Parse(ini);
            var str = obj.ToString();

            Assert.That(str, Does.Contain("[section]"));
            Assert.That(str, Does.Contain("key = value"));
        }

        // ─── IniSerializerAttribute tests ────────────────────────────────────────

        [Test]
        public void ini_serializer_attribute_throws_for_invalid_type()
        {
            Assert.Throws<System.ArgumentException>(() =>
                _ = new IniSerializerAttribute(typeof(string))
            );
        }

        [Test]
        public void ini_serializer_attribute_accepts_valid_serializer()
        {
            Assert.DoesNotThrow(() =>
                _ = new IniSerializerAttribute(typeof(IntSerializer))
            );
        }

        // ─── IniDataParser.ParseAs<T> tests ──────────────────────────────────────

        [Test]
        public void data_parser_parse_as_returns_typed_object()
        {
            const string ini = @"
[Server]
host = parseas.test
port = 7777
enabled = true
";
            var parser = new IniDataParser();
            var cfg = parser.ParseAs<AppConfig>(ini);

            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg!.Server.Host, Is.EqualTo("parseas.test"));
            Assert.That(cfg.Server.Port, Is.EqualTo(7777));
        }
    }
}
