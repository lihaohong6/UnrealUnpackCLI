# UnrealUnpackCLI

This is a command-line utility for exporting assets from Unreal Engine games.

Since [FModel](https://github.com/4sval/FModel) only provides a graphical user interface, repeatedly bulk-exporting assets becomes labor-intensive. This project parses command line arguments and then calls [CUE4Parse](https://github.com/FabianFG/CUE4Parse) to do the heavy lifting.

I have not written a single line of C# before this project, so you may see code with questionable quality.

## Install

Either download the binary from the [Releases](https://github.com/lihaohong6/UnrealUnpackCLI/releases) section or build the project yourself with
```shell
dotnet build --configuration Release
```
All you need to do is to run the executable file in the terminal. No installation process necessary.

## Examples
```
.\AutoUnpack.exe -t json -k <AES key here> -i <path to some game> -o json_export JsonFiles PM/Content
.\AutoUnpack.exe -t png -k <AES key here> -i <path to some game> -o png_export DynamicResource/Emote PM/Content/PaperMan/UI/Atlas/DynamicResource DynamicResource/Item/ItemIcon PM/Content/PaperMan/UI/Atlas/DynamicResource
.\AutoUnpack.exe -t bin -k <AES key here> -i <path to some game> -o audio_export WwiseAudio/Windows PM/Content/WwiseAudio/Windows
```

### Explanation

The `-t` argument specifies the export type. Only `json`, `png`, and binary files are supported.

The `-k` argument specifies the AES key for decrypting the package.

The `-i` argument is the game directory where `pak` files can be found.

The `-o` argument is where exports will be stored.

The remaining positional arguments appear in pairs of `filter replace`. Only directories that contain the string in `filter` will be exported. In the export process, the part of the directory that contains the string in `replace` will be truncated to reduce the amount of nesting in the directory structure. 

For example, if the `-o` argument is `json_files`, `filter` is `JsonFiles`, `replace` is `PM/Content`, and there is a file under `PM/Content/JsonFiles/a.json`. `a.json` will be exported under `json_files/JsonFiles/a.json`. If `replace` is `DONOTREPLACE` instead, `a.json` would be under `json_files/PM/Content/JsonFiles/a.json`. 

## Audio processing

> [!NOTE]
> This section only applies to a specific game. To deal with `wem` files in other games, you will need to modify the script significantly. You may find [vgmstream](https://github.com/vgmstream/vgmstream) and [wwiser](https://github.com/bnnm/wwiser) helpful.

Exported audio files can be further processed by `audio.py` in the `audio` directory. Example:

```
python audio.py <path to Windows directory in audio export>
```

The program assumes that the `Windows` directory contains the `Chinese` and `Japanese` subdirectory. It'll then automatically generate `bank.xml` files and extract `wav` files. To extract `wav` files `vgmstream-cli` must be available in `PATH`.

The audio processing utility embeds [wwiser](https://github.com/bnnm/wwiser) for convenience's sake.