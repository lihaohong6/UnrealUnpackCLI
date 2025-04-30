using CUE4Parse_Conversion.Textures;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Localization;
using Newtonsoft.Json;
using SkiaSharp;

namespace AutoUnpack;

public class Unpacker(DefaultFileProvider provider, bool multithreaded = true) {
    private int _taskCount;
    private int _progress;
    private readonly HashSet<string> _unpackedFiles = [];

    ~Unpacker() {
        if (multithreaded) {
            Wait();
        }
    }

    private void RunTask(Action f) {
        if (multithreaded) {
            ThreadPool.QueueUserWorkItem(_ => { 
                f.Invoke();
                Interlocked.Increment(ref _progress);
            });
            _taskCount++;
        }
        else {
            f.Invoke();
        }
    }

    public void Wait() {
        Console.WriteLine($"Waiting for {_taskCount} tasks. ");
        while (_progress < _taskCount) {
            Thread.Sleep(100);
            Console.WriteLine($"Completed {_progress} out of {_taskCount}");
        }
    }
    
    private bool CheckFile(string fullDirectory) {
        Directory.GetParent(fullDirectory)?.Create();
        return File.Exists(fullDirectory);
    }

    public void ProcessPng(string exportRoot, string f, string truncate) {
        var path = f.Split(".")[0];
        var objects = provider.LoadPackage(path).GetExports();
        foreach (var obj in objects) {
            switch (obj) {
                case UTexture2D texture: {
                    var filePath = path.Replace(truncate, "") + ".png";
                    var success = _unpackedFiles.Add(filePath);
                    if (!success) {
                        // This file is a dup
                        break;
                    }
                    var fullDirectory = exportRoot + filePath;
                    Directory.GetParent(fullDirectory)?.Create();
                    var decoded = texture.Decode();
                    
                    RunTask(() => {
                        var exported = decoded?.Encode(SKEncodedImageFormat.Png, 100)?.ToArray();

                        if (exported == null) {
                            Console.WriteLine("Failed to export texture " + path);
                            return;
                        }
                    
                        if (CheckFile(fullDirectory)) {
                            var existing = File.ReadAllBytes(fullDirectory);
                            if (existing.SequenceEqual(exported)) {
                                return;
                            }
                        }
                    
                        using (var fileStream = new FileStream(fullDirectory, FileMode.Create)) {
                            fileStream.Write(exported);
                        }

                        Console.WriteLine(filePath + " exported");
                    });
                    break;
                }
            }
        }
    }

    public void ProcessJson(string csvRoot, string fullPath, string truncate) {
        string fullJson;
        string path = fullPath.Split(".")[0];
        if (fullPath.EndsWith(".uasset") || fullPath.EndsWith(".uexp")) {
            try {
                if (provider.TryLoadPackageObject(path, out var uObject)) {
                    fullJson = JsonConvert.SerializeObject(uObject, Formatting.Indented);
                }
                else {
                    var allObjects = provider.LoadPackage(path).GetExports();
                    var lst = allObjects.ToList();
                    fullJson = lst.Count == 1 ? 
                        JsonConvert.SerializeObject(lst.First(), Formatting.Indented) : 
                        JsonConvert.SerializeObject(lst, Formatting.Indented);
                }
            }
            catch (Exception e) {
                // ignored
                Console.WriteLine("Failed for " + path);
                Console.WriteLine(e);
                return;
            }
        }
        else if (fullPath.Contains(".locres")) {
            var file = provider.Files[fullPath];
            file.TryCreateReader(out var archive);
            var locres = new FTextLocalizationResource(archive);
            fullJson = JsonConvert.SerializeObject(locres, Formatting.Indented);
        }
        else {
            Console.WriteLine("Cannot recognize " + fullPath);
            return;
        }

        path = path.Replace(truncate, "") + ".json";
        var fullDirectory = csvRoot + path;
        if (CheckFile(fullDirectory) && File.ReadAllText(fullDirectory) == fullJson) {
            return;
        }

        File.WriteAllText(fullDirectory, fullJson);
        Console.WriteLine("Written to " + path);
    }

    public void ProcessBinaryFiles(string outputRoot, string path, string truncate) {
        var filePath = path.Replace(truncate, "");
        var fullDirectory = outputRoot + filePath;

        var obj = provider.SaveAsset(path);

        if (CheckFile(fullDirectory)) {
            var existing = File.ReadAllBytes(fullDirectory);
            if (existing.SequenceEqual(obj)) {
                return;
            }
        }

        using (var fileStream = new FileStream(fullDirectory, FileMode.Create)) {
            fileStream.Write(obj);
        }
        
        Console.WriteLine("Written to " + filePath);
    }
}