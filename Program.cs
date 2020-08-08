using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace csmake
{
    enum BuildableTarget
    {
        Console,        //a console/cmd app
    };

    class Package
    {
        //e.g.       "nuget;newtonsoft.json;12.0.3",
        public Package(string desc)
        {
            var parts = desc.Split(';');
            Repository = parts[0];
            Name = parts[1];
            Version = parts[2];
        }

        public Package()
        {

        }

        public string Repository;
        public string Name;
        public string Version;
    };


    class BuildableModel
    {
        BuildableTarget Type;
        //what framework 
        public string Framework;
        //source files
        public List<string> Sources;
        //packages used
        public List<Package> Packages;
        //preferred output directory
        public string OutputDir;

        public string GlobSources()
        {
            var sb = new StringBuilder();
            foreach(var s in Sources)
            {
                sb.Append(s);
            }
            return sb.ToString();
        }
    }

    //defines all the values that can be set in a .csmake config file
    class Config
    {
        //default values in here define default values for csmake, to be overriden by a .csmake file
        public string Type = "console";
        public string Framework = "netcoreapp3.1";
        public List<string> Sources;

        public Config()
        {
            Sources = new List<string>() { "*.cs" };
        }
    }


    
    class Program
    {
        static private void GenerateNetCoreApp(string name, BuildableModel b)
        {
            var xml =
            new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                new XElement("PropertyGroup",
                    new XElement("OutputType", "Exe"),
                    new XElement("OutputPath", b.OutputDir),
                    new XElement("TargetFramework", b.Framework)
                ),
                new XElement("PropertyGroup", 
                    new XElement("EnableDefaultCompileItems", false)
                ),
                
                new XElement("ItemGroup",
                    new XElement("Compile", new XAttribute("Include", b.GlobSources()), new XAttribute("Exclude", "$(DefaultItemExcludes);$(DefaultExcludesInProjectFolder)"))
                ),
                new XElement("ItemGroup",
                    from p in b.Packages select new XElement("PackageReference", new XAttribute("Include", p.Name), new XAttribute("Version", p.Version))
                )
            );

            var result = xml.ToString();
            System.IO.File.WriteAllText("test.csproj", result);
            System.Diagnostics.Trace.WriteLine(result);
        }

        static void Init(Config conf, string[] args)
        {
            //write a starter project out, merging between args and conf for values
            var proj =
            new JObject(
                new JProperty(args[0], 
                    new JObject(
                        new JProperty("type", conf.Type),
                        new JProperty("framework", conf.Framework),
                        new JProperty("sources", new JArray(from s in conf.Sources select new JValue(s))),
                        new JProperty("packages", new JArray())
                    )
                )
            );

            System.IO.File.WriteAllText($"{args[0]}.json", proj.ToString());
        }

        static void Generate(Config conf, string[] args)
        {
            var buildables = JsonConvert.DeserializeObject<Dictionary<string, BuildableModel>>(
                System.IO.File.ReadAllText(args[0])
            );

            foreach (var bld in buildables)
            {
                var b = bld.Value;
                switch (b.Framework)
                {
                    case "netcoreapp3.1":
                        GenerateNetCoreApp(bld.Key, b);
                        break;
                }
            }
        }

        static Config AssembleConfig()
        {
            //start in the current directory, navigate up to root and look for .csmake files
            var dir = Environment.CurrentDirectory;
            var confs = new List<string>();

            while (dir != null)
            {
                var f = dir.EndsWith("\\") ? $"{dir}.csmake" : $"{dir}\\.csmake";
                if(System.IO.File.Exists(f))
                {
                    //collect up results
                    confs.Add(f);
                }
                //go up a directory until root
                var up = System.IO.Directory.GetParent(dir);
                dir = (up != null) ? up.FullName : null;
            }

            //walk configs list backwards, applying on top of existing object
            var c = new Config();
            foreach(var f in confs)
            {
                JsonConvert.PopulateObject(System.IO.File.ReadAllText(f), c);
            }
            
            return c;
        }

        static void Main(string[] args)
        {
            var conf = AssembleConfig();

            switch (args[0])
            {
                case "init":
                    Init(conf, args.Skip(1).ToArray());
                    break;
                case "gen":
                case "generate":
                    Generate(conf, args.Skip(1).ToArray());
                    break;
                case "build":
                    break;
            }
            
            return;
        }
    }
}
