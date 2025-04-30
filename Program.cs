using CommandLine;
using CUE4Parse.Compression;

namespace AutoUnpack {
public static class Program {

    private static List<(string, string)> DynamicResourceToPngRules(List<string> rules) {
        return rules.Select(rule => ("PM/Content/PaperMan/UI/Atlas/DynamicResource/" + rule, "PM/Content/PaperMan/UI/Atlas")).ToList();
    }
    
    private static void DumpData(string providerRoot, string exportRoot, string jsonRoot,
        List<(string, string)> jsonRules, List<(string, string)> pngRules, List<(string, string)> binaryRules) {
        var provider = Utilities.GetProvider(providerRoot);
        var unpacker = new Unpacker(provider);
        foreach (var f in provider.Files.Keys) {
            foreach (var (pattern, replace) in jsonRules) {
                if (f.Contains(pattern)) {
                    unpacker.ProcessJson(jsonRoot, f, replace);
                    break;
                }
            }
            foreach (var (pattern, replace) in pngRules) {
                if (f.Contains(pattern)) {
                    unpacker.ProcessPng(exportRoot, f, replace);
                    break;
                }
            }
            foreach (var (pattern, replace) in binaryRules) {
                if (f.Contains(pattern)) {
                    unpacker.ProcessBinaryFiles(exportRoot, f, replace);
                    break;
                }
            }
        }
        unpacker.Wait();
    }

    private static List<(string, string)> GetPngRules() {
        List<string> dynamicResources = [
            "Item/ItemIcon",
            "Item/BigIcon",
            "Emote",
            "Weapon/InGameGrowth",
            "Weapon/WeaponIconWhite",
            "Achievement",
            "Skill",
            "Store",
            "RoleSkin/RoleProfile",
            "RoleSkin/RoleHUD",
            "Decal",
            "IdCard",
            "Map/Introduce",
            "Map/Mini2D",
            "BattlePass/Background",
            "RoguelikeCard",
            "Talent"
        ];
        const string 
            APARTMENT_ROOT = "PM/Content/PaperMan/Environment/Textures/Maps/Apartment/", 
            APARTMENT_TRUNCATION = "PM/Content/PaperMan/Environment/Textures/Maps";
        var pngRules = new List<(string, string)> {
            (APARTMENT_ROOT + "BP-AVG-CG",
                APARTMENT_TRUNCATION),
            (APARTMENT_ROOT + "BP-AVG-BJ",
                APARTMENT_TRUNCATION),
            (APARTMENT_ROOT + "Pledge",
                APARTMENT_TRUNCATION),
            (APARTMENT_ROOT + "Background",
                APARTMENT_TRUNCATION),
            ("PM/Content/PaperMan/UI/Atlas/PC/Frontend/Activities/MidAutumnPC/NotPack",
                "PM/Content/PaperMan/UI/Atlas/PC")
        };
        pngRules.AddRange(DynamicResourceToPngRules(dynamicResources));
        return pngRules;
    }
    
    private static void DumpChineseData(string providerRoot, string exportRoot, string csvRoot) {
        
        var jsonRules = new List<(string, string)> {
            ("PM/Content/PaperMan/CSV", "PM/Content/PaperMan"),
            ("PM/Content/PaperMan/CyTable", "PM/Content/PaperMan"),
            ("PM/Content/WwiseAssets/AkEvent", "PM/Content"),
        };
        var pngRules = GetPngRules();
        var audioRules = new List<(string, string)> {
            ("PM/Content/WwiseAudio", "PM/Content"),
        };
        DumpData(providerRoot, exportRoot, csvRoot, jsonRules, pngRules, audioRules);
    }

    private static void DumpGlobalData(string providerRoot, string exportRoot, string jsonRoot) {
        var jsonRules = new List<(string, string)> {
            ("PM/Content/PaperMan/CSV", "PM/Content/PaperMan"),
            ("PM/Content/Localization/Game", "PM/Content"),
            ("PM/Content/WwiseAssets/AkEvent", "PM/Content")
        };
        var audioRules = new List<(string, string)> {
            ("PM/Content/WwiseAudio", "PM/Content")
        };
        var pngRules = GetPngRules();
        DumpData(providerRoot, exportRoot, jsonRoot, jsonRules, pngRules, audioRules);
    }

    private static void DumpAllJson(string providerRoot) {
        var provider = Utilities.GetProvider(providerRoot);
        var unpacker = new Unpacker(provider);
        List<string> keys = [];
        keys.AddRange(provider.Files.Keys.Where(
            key => key.Contains("PM/Content/PaperMan")
            // && !key.Contains("PaperMan/Maps") &&
            // !key.Contains("PaperMan/SkinAssets") &&
            // !key.Contains("/Cinematics/")
        ));
        // keys.AddRange(provider.Files.Keys.Where(key => !key.Contains("PM/Content/PaperMan")));
        keys.Sort();
        Console.WriteLine("{0} total", keys.Count);
        var i = 3524; // 3524 causes a crash
        while (i < keys.Count) {
            // Console.WriteLine(i);
            // Console.WriteLine(keys[i]);
            var f = keys[i];
            var path = f.Split(".")[0];
            unpacker.ProcessJson("allJson/", path, "DO_NOT_TRUNCATE");
            i++;
            if (i % 1000 == 0) {
                Console.WriteLine("{0} out of {1}", i, keys.Count);
            }
        }
    }

    private static void Other(string providerRoot, string exportRoot) {
        Playground.Test(providerRoot, exportRoot);
    }

    class Options {
        [Option('t', "type", Required = true, HelpText = "The type of file to export (json, png, binary).")]
        public required string Type { get; set; }
        
        [Option('i', "input", Required = true, HelpText = "Where to get game files.")]
        public required string Input { get; set; }

        [Option('o', "output", Default = ".", HelpText = "Where to store output files.")]
        public required string Output { get; set; }
        
        [Option('k', "key", Required = true, HelpText = "Where to store output files.")]
        public required string Key {get; set; }

        [Value(0, Required = true,
            HelpText =
                "Pairs of strings where the first denotes the file name pattern to export " +
                "while the second denotes the part of the path to ignore/truncate.")]
        public required IEnumerable<string> Exports { get; set; }
    }

    public static void Main(string[] args) {
        if (args.Length == 0) {
            Console.WriteLine("Need a command");
            return;
        }

        ZlibHelper.DownloadDll();
        ZlibHelper.Initialize(ZlibHelper.DLL_NAME);

        switch (args[0]) {
            case "CN": {
                DumpChineseData("""D:\Games\CalabiYau\CalabiyauGame""",
                    "D:/Strinova/AutoUnpack/CNExport",
                    "D:/Strinova/Strinova-data/CN");
                break;
            }
            case "CN-beta": {
                DumpChineseData("""D:\Games\Beta\CalabiYau\CalabiYau\CalabiyauGame""",
                    "D:/Strinova/AutoUnpack/CNExport",
                    "D:/Strinova/Strinova-data/CN");
                break;
            }
            case "GL": {
                DumpGlobalData("D:/Games/Strinova/Game",
                    "D:/Strinova/AutoUnpack/GLExport",
                    "D:/Strinova/Strinova-data/Global");
                break;
            }
            case "GL-beta": {
                DumpGlobalData("D:/Games/Beta/Strinova_TestServer/Strinova/Game",
                    "D:/Strinova/AutoUnpack/GLExport",
                    "D:/Strinova/Strinova-data/Global");
                break;
            }
            case "DumpAll": {
                DumpAllJson("""D:\Games\CalabiYau\CalabiyauGame""");
                break;
            }
            case "Other": {
                Other("D:/Games/Strinova/Game",
                    "D:/Strinova/AutoUnpack/GLExport");
                break;
            }
            default: {
                CustomCommand(args);
                break;
            }
        }

        Console.WriteLine("Program complete");
    }

    private static void CustomCommand(string[] args) {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(obj => {
                var inputDirectory = obj.Input;
                Utilities.CheckProviderRoot(inputDirectory);
                var provider = Utilities.GetProvider(inputDirectory, obj.Key);
                var unpacker = new Unpacker(provider);
                var d = new Dictionary<string, Action<string, string, string>> {
                    { "png", unpacker.ProcessPng },
                    { "json", unpacker.ProcessJson },
                    { "bin", unpacker.ProcessBinaryFiles }
                };
                var command = obj.Type;
                if (!d.ContainsKey(command)) {
                    Console.WriteLine("Unknown export type \"" + command + "\"");
                    return;
                }
                var action = d[command];
                var filters = obj.Exports.Where((x, i) => i % 2 == 0).ToList();
                var replacements = obj.Exports.Where((x, i) => i % 2 == 1).ToList();
                if (filters.Count != replacements.Count) {
                    Console.WriteLine("Filters and replacements don't match!");
                }

                foreach (var f in provider.Files.Keys) {
                    for (var i = 0; i < filters.Count; i++) {
                        if (f.Contains(filters[i])) {
                            action(obj.Output, f, replacements[i]);
                            break;
                        }
                    }
                }
                unpacker.Wait();
            })
            .WithNotParsed(HandleParseError);
    }

    private static void HandleParseError(IEnumerable<Error> obj) {
        foreach (var o in obj) {
            Console.WriteLine(o.ToString());
        }
    }
} // class Program
} // namespace AutoUnpack