using CUE4Parse_Conversion.Textures;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Texture;
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
            var path = f.Split(".")[0];
            List<string> dynamicResources = [
                "PM/Content/PaperMan/UI/Atlas/DynamicResource/Item/ItemIcon",
                "PM/Content/PaperMan/UI/Atlas/DynamicResource/Emote"
            ];
            if (f.Contains("PM/Content/PaperMan/CSV")) {
                ProcessJson(csvRoot, provider, path, "PM/Content/PaperMan");
            }
            else if (f.Contains("PM/Content/WwiseAssets/AkEvent")) {
                ProcessJson(exportRoot, provider, path, "PM/Content");
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
            else if (dynamicResources.Any(s => path.Contains(s))) {
                var obj = provider.LoadObject(path);
                switch (obj) {
                    case UTexture2D texture: {
                        var fullDirectory = exportRoot +
                                            path.Replace("PM/Content/PaperMan/UI/Atlas/DynamicResource", "") +
                                            ".png";
                        Directory.GetParent(fullDirectory)?.Create();
                        if (CheckFile(fullDirectory)) {
                            continue;
                        }
                        
                        var bitmap = texture.Decode()?.Encode(SKEncodedImageFormat.Png, 100);
                        using (var fileStream = new FileStream(fullDirectory, FileMode.Create)) {
                            bitmap?.SaveTo(fileStream);
                        }
                        
                        break;
                    }
                }
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
            var path = f.Split(".")[0];
            if (f.Contains("PM/Content/PaperMan/CSV")) {
                ProcessJson(jsonRoot, provider, path, "PM/Content/PaperMan");
            }
            else if (f.Contains("PM/Content/Localization/Game")) {
                ProcessJson(jsonRoot, provider, path, "PM/Content/Localization");
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

    private static void ProcessJson(string csvRoot, DefaultFileProvider provider, string path, string truncate) {
        try {
            var allObjects = provider.LoadObject(path);
            var fullDirectory = csvRoot + path.Replace(truncate, "") + ".json";
            var fullJson = JsonConvert.SerializeObject(allObjects, Formatting.Indented);
            if (CheckFile(fullDirectory) && File.ReadAllText(fullDirectory) == fullJson) {
                return;
            }

            File.WriteAllText(fullDirectory, fullJson);
        }
        catch (Exception) {
            // ignored
        }
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

    public static void Main(string[] args) {
        if (args.Length == 0) {
            Console.WriteLine("Need arg to specify server.");
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
                DumpGlobalData("D:/Strinova/Paks",
                    "D:/Strinova/AutoUnpack/GLExport",
                    "D:/Strinova/Strinova-data/Global");
                break;
            }
            case "Other": {
                DumpAllJson("""D:\Games\CalabiYau\CalabiyauGame""");
                break;
            }
        }
    }
} // class Program
} // namespace AutoUnpack