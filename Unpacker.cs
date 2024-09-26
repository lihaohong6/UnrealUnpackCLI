using CUE4Parse_Conversion.Textures;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using SkiaSharp;

namespace AutoUnpack;

public static class Unpacker {
    private const string AesKey = "0x7456667CCC6BF87AAD3DAA2DDAC3B02564C5B9D74565BB36645C46AC210CDE40";
    
    private static bool CheckFile(string fullDirectory) {
        Directory.GetParent(fullDirectory)?.Create();
        return File.Exists(fullDirectory);
    }
    
    public static DefaultFileProvider GetProvider(string providerRoot) {
        var provider = new DefaultFileProvider(providerRoot, SearchOption.AllDirectories,
            false, new VersionContainer(EGame.GAME_UE4_25));
        provider.Initialize(); // will scan the archive directory for supported file extensions
        provider.SubmitKey(new FGuid(), new FAesKey(AesKey));
        provider.Mount();
        return provider;
    }

    public static void ProcessPng(string exportRoot, DefaultFileProvider provider, string f, string truncate,
        bool force = false) {
        var path = f.Split(".")[0];
        var objects = provider.LoadAllObjects(path);
        foreach (var obj in objects) {
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
    }
    

    public static void ProcessJson(string csvRoot, DefaultFileProvider provider, string path, string truncate,
        bool force = false) {
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

    public static void ProcessAudio(string outputRoot, DefaultFileProvider provider, string path, string truncate,
        bool force = false) {
        var filePath = path.Replace(truncate, "");
        var fullDirectory = outputRoot + filePath;
        if (!force && CheckFile(fullDirectory)) {
            return;
        }

        var obj = provider.SaveAsset(path);

        using (var fileStream = new FileStream(fullDirectory, FileMode.Create)) {
            fileStream.Write(obj);
        }
        
        Console.WriteLine("Written to " + filePath);
    }
}