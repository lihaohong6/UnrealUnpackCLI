using CommandLine;
using CUE4Parse.FileProvider;

namespace AutoUnpack {
public static class Program {
    private static void DumpChineseData(string providerRoot, string exportRoot, string csvRoot) {
        var provider = Utilities.GetProvider(providerRoot);
        var unpacker = new Unpacker(provider);
        foreach (var f in provider.Files.Keys) {
            List<string> dynamicResources = [
                "Item/ItemIcon",
                "Item/BigIcon",
                "Emote",
                "Weapon/InGameGrowth",
                "Achievement",
                "Skill",
                "Store",
                "RoleSkin/RoleProfile",
                "RoleSkin/RoleHUD",
                "Decal",
                "IdCard",
                "Map/Introduce",
                "Map/Mini2D"
            ];
            var jsonRules = new List<(string, string)> {
                ("PM/Content/PaperMan/CSV", "PM/Content/PaperMan"),
                ("PM/Content/PaperMan/CyTable", "PM/Content/PaperMan"),
                ("PM/Content/WwiseAssets/AkEvent", "PM/Content"),
            };
            var otherPngRules = new List<(string, string)> {
                ("PM/Content/PaperMan/Environment/Textures/Maps/Apartment/BP-AVG-CG",
                    "PM/Content/PaperMan/Environment/Textures/Maps"),
                ("PM/Content/PaperMan/Environment/Textures/Maps/Apartment/Pledge",
                    "PM/Content/PaperMan/Environment/Textures/Maps"),
            };
            foreach (var (pattern, replace) in jsonRules) {
                if (f.Contains(pattern)) {
                    unpacker.ProcessJson(csvRoot, f, replace);
                    break;
                }
            }

            if (f.Contains("PM/Content/WwiseAudio")) {
                unpacker.ProcessAudio(exportRoot, f, "PM/Content");
            }
            else if (dynamicResources.Any(s => f.Contains("PM/Content/PaperMan/UI/Atlas/DynamicResource/" + s))) {
                unpacker.ProcessPng(exportRoot, f, "PM/Content/PaperMan/UI/Atlas");
            }

            foreach (var (pattern, replace) in otherPngRules) {
                if (f.Contains(pattern)) {
                    unpacker.ProcessPng(exportRoot, f, replace);
                }
            }
        }
        unpacker.Wait();
    }

    private static void DumpGlobalData(string providerRoot, string exportRoot, string jsonRoot) {
        var provider = Utilities.GetProvider(providerRoot);
        var unpacker = new Unpacker(provider);
        foreach (var f in provider.Files.Keys) {
            var jsonRules = new List<(string, string)> {
                ("PM/Content/PaperMan/CSV", "PM/Content/PaperMan"),
                ("PM/Content/Localization/Game", "PM/Content"),
                ("PM/Content/WwiseAssets/AkEvent", "PM/Content")
            };
            foreach (var (pattern, replace) in jsonRules) {
                if (f.Contains(pattern)) {
                    unpacker.ProcessJson(jsonRoot, f, replace);
                    break;
                }
            }

            if (f.Contains("PM/Content/WwiseAudio/Windows/English")) {
                unpacker.ProcessAudio(exportRoot, f, "PM/Content");
            }
        }
        unpacker.Wait();
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

    private static void Other() {
        var provider = Utilities.GetProvider("D:/Games/Strinova/Game");
        var unpacker = new Unpacker(provider);
        unpacker.ProcessJson("D:/Strinova/Strinova-data/Global", "PM/Content/WwiseAssets/AkEvent/Vox_Huixing_Cry.uasset", "PM/Content");
    }

    class Options {
        [Option('i', "input", Required = true, HelpText = "Where to get game files.")]
        public string input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Where to store output files.")]
        public string output { get; set; }

        [Value(0, Required = true,
            HelpText =
                "Pairs of strings where the first denotes the file name pattern to export " +
                "while the second denotes the part of the path to ignore/truncate.")]
        public IEnumerable<string> exports { get; set; }
    }

    public static void Main(string[] args) {
        if (args.Length == 0) {
            Console.WriteLine("Need a command");
            return;
        }

        switch (args[0]) {
            case "CN": {
                DumpChineseData("""D:\Games\CalabiYau\CalabiyauGame""",
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
            case "DumpAll": {
                DumpAllJson("""D:\Games\CalabiYau\CalabiyauGame""");
                break;
            }
            case "Other": {
                Other();
                break;
            }
            default: {
                CustomCommand(args, args[0]);
                break;
            }
        }

        Console.WriteLine("Program complete");
    }

    private static void CustomCommand(string[] args, string command) {
        var args2 = new string[args.Length - 1];
        Array.Copy(args, 1, args2, 0, args.Length - 1);
        CommandLine.Parser.Default.ParseArguments<Options>(args2)
            .WithParsed(obj => {
                var provider = Utilities.GetProvider(obj.input);
                var unpacker = new Unpacker(provider);
                var d = new Dictionary<string, Action<string, string, string>> {
                    { "png", unpacker.ProcessPng },
                    { "json", unpacker.ProcessJson },
                    { "audio", unpacker.ProcessAudio }
                };
                if (!d.ContainsKey(command)) {
                    Console.WriteLine("Unknown command \"" + command + "\"");
                    return;
                }
                var action = d[command];
                var filters = obj.exports.Where((x, i) => i % 2 == 0).ToList();
                var replacements = obj.exports.Where((x, i) => i % 2 == 1).ToList();
                if (filters.Count != replacements.Count) {
                    Console.WriteLine("Filters and replacements don't match!");
                }

                foreach (var f in provider.Files.Keys) {
                    for (var i = 0; i < filters.Count; i++) {
                        if (f.Contains(filters[i])) {
                            action(obj.output, f, replacements[i]);
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