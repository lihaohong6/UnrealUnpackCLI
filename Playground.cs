using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

namespace AutoUnpack;

public class Playground {
    public static void Test(string providerRoot, string exportRoot) {
        var provider = Utilities.GetProvider(providerRoot);
        var objects = provider.LoadAllObjects("PM/Content/PaperMan/SkinAssets/Characters/MoBai/S001/Mesh3D/MoBai_Mesh");
        foreach (var obj in objects) {
            switch (obj) {
                case UMorphTarget morphTarget: {
                    break;
                }
                case USkeletalMesh mesh: {
                    var options = new ExporterOptions();
                    options.TextureFormat = ETextureFormat.Png;
                    options.MeshFormat = EMeshFormat.Gltf2;
                    options.LodFormat = ELodFormat.FirstLod;
                    options.ExportMorphTargets = true;
                    var exporter = new MeshExporter(mesh, options);
                    string o1, o2;
                    bool result = exporter.TryWriteToDir(new DirectoryInfo(exportRoot + "/models"), out o1, out o2);
                    break;
                }
                default: {
                    Console.WriteLine("Error: Unknown object type");
                    break;
                }
            }
        }
    }
}