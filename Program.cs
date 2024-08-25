using System.Diagnostics;
using CommandLine;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using SkiaSharp;

namespace AutoUnpack {
public static class Program {
    private const string AesKey = "0x7456667CCC6BF87AAD3DAA2DDAC3B02564C5B9D74565BB36645C46AC210CDE40";

    private static void DumpChineseData(string providerRoot, string exportRoot, string csvRoot) {
        var provider = GetProvider(providerRoot);
        foreach (var f in provider.Files.Keys) {
            List<string> dynamicResources = [
                "PM/Content/PaperMan/UI/Atlas/DynamicResource/Item/ItemIcon",
                "PM/Content/PaperMan/UI/Atlas/DynamicResource/Emote"
            ];
            if (f.Contains("PM/Content/PaperMan/CSV")) {
                ProcessJson(csvRoot, provider, f, "PM/Content/PaperMan");
            }
            else if (f.Contains("PM/Content/WwiseAssets/AkEvent")) {
                ProcessJson(exportRoot, provider, f, "PM/Content");
                ProcessJson(csvRoot, provider, f, "PM/Content");
            }
            else if (f.Contains("PM/Content/WwiseAudio")) {
                var fullDirectory = exportRoot + f.Replace("PM/Content", "");
                if (CheckFile(fullDirectory)) {
                    continue;
                }

                var obj = provider.SaveAsset(f);

                using (var fileStream = new FileStream(fullDirectory, FileMode.Create)) {
                    fileStream.Write(obj);
                }
            }
            else if (dynamicResources.Any(s => f.Contains(s))) {
                ProcessPng(exportRoot, provider, f, "PM/Content/PaperMan/UI/Atlas/DynamicResource");
            }
        }
    }

    private static void ProcessPng(string exportRoot, DefaultFileProvider provider, string f, string truncate,
        bool force = false) {
        var path = f.Split(".")[0];
        var obj = provider.LoadObject(path);
        switch (obj) {
            case UTexture2D texture: {
                var filePath = path.Replace(truncate, "") + ".png";
                var fullDirectory = exportRoot + filePath;
                Directory.GetParent(fullDirectory)?.Create();
                if (!force && CheckFile(fullDirectory)) {
                    return;
                }

                var bitmap = texture.Decode()?.Encode(SKEncodedImageFormat.Png, 100);
                using (var fileStream = new FileStream(fullDirectory, FileMode.Create)) {
                    bitmap?.SaveTo(fileStream);
                }

                Console.WriteLine(filePath + " exported");
                break;
            }
        }
    }

    private static bool CheckFile(string fullDirectory) {
        Directory.GetParent(fullDirectory)?.Create();
        return File.Exists(fullDirectory);
    }

    private static void DumpGlobalData(string providerRoot, string exportRoot, string jsonRoot) {
        var provider = GetProvider(providerRoot);
        foreach (var f in provider.Files.Keys) {
            if (f.Contains("PM/Content/PaperMan/CSV")) {
                ProcessJson(jsonRoot, provider, f, "PM/Content/PaperMan");
            }
            else if (f.Contains("PM/Content/Localization/Game")) {
                ProcessJson(jsonRoot, provider, f, "PM/Content");
            }
        }
    }

    private static DefaultFileProvider GetProvider(string providerRoot) {
        var provider = new DefaultFileProvider(providerRoot, SearchOption.AllDirectories,
            false, new VersionContainer(EGame.GAME_UE4_25));
        provider.Initialize(); // will scan the archive directory for supported file extensions
        provider.SubmitKey(new FGuid(), new FAesKey(AesKey));
        provider.Mount();
        return provider;
    }

    private static void ProcessJson(string csvRoot, DefaultFileProvider provider, string path, string truncate, bool force = false) {
        string fullJson;
        if (path.EndsWith(".uasset") || path.EndsWith(".uexp")) {
            try {
                var allObjects = provider.LoadObject(path.Split(".")[0]);
                fullJson = JsonConvert.SerializeObject(allObjects, Formatting.Indented);
            }
            catch (Exception) {
                // ignored
                Console.WriteLine("Failed for " + path);
                return;
            }
        }
        else if (path.Contains(".locres")) {
            var file = provider.Files[path];
            file.TryCreateReader(out var archive);
            var locres = new FTextLocalizationResource(archive);
            fullJson = JsonConvert.SerializeObject(locres, Formatting.Indented);
        }
        else {
            Console.WriteLine("Cannot recognize " + path);
            return;
        }

        path = path.Split(".")[0];
        path = path.Replace(truncate, "") + ".json";
        var fullDirectory = csvRoot + path;
        if (CheckFile(fullDirectory) && File.ReadAllText(fullDirectory) == fullJson) {
            return;
        }

        File.WriteAllText(fullDirectory, fullJson);
        Console.WriteLine("Written to " + path);
    }

    private static void DumpAllJson(string providerRoot) {
        var provider = GetProvider(providerRoot);
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
            ProcessJson("allJson/", provider, path, "DO_NOT_TRUNCATE");
            i++;
            if (i % 1000 == 0) {
                Console.WriteLine("{0} out of {1}", i, keys.Count);
            }
        }
    }

    class Options {
        [Option('i', "input", Required = true, HelpText = "Where to get game files.")]
        public string input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Where to store output files.")]
        public string output { get; set; }

        [Option('f', "filter", Required = true, HelpText = "Only export files with this string in its path.")]
        public IEnumerable<string> filter { get; set; }

        [Option('r', "replace", Required = true, HelpText = "Segment of the file path to replace.")]
        public IEnumerable<string> replace { get; set; }

        [Option("force", Required = false, Default = false,
            HelpText = "Overwrite file even if one already exists. Only works for png.")]
        public bool force { get; set; }
    }

    static void Main(string[] args) {
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
            case "json": {
                CustomCommand(args, ProcessJson);
                break;
            }
            case "png": {
                CustomCommand(args, ProcessPng);
                break;
            }
        }

        Console.WriteLine("Program complete");
    }

    private static void CustomCommand(string[] args, Action<string, DefaultFileProvider, string, string, bool> action) {
        var args2 = new string[args.Length - 1];
        Array.Copy(args, 1, args2, 0, args.Length - 1);
        CommandLine.Parser.Default.ParseArguments<Options>(args2)
            .WithParsed(obj => {
                var provider = GetProvider(obj.input);
                var filters = new List<string>(obj.filter);
                var replacements = new List<string>(obj.replace);
                foreach (var f in provider.Files.Keys) {
                    for (var i = 0; i < filters.Count; i++) {
                        if (f.Contains(filters[i])) {
                            action(obj.output, provider, f, replacements[i], obj.force);
                            break;
                        }
                    }
                }
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