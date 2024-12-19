This is a command-line utility for exporting assets from Unreal Engine games. 

Since [FModel](https://github.com/4sval/FModel) only provides a graphical user interface, repeatedly bulk-exporting assets becomes labor-intensive. This project parses command line arguments and then calls [CUE4Parse](https://github.com/FabianFG/CUE4Parse) to do the heavy lifting.

I have not written a single line of C# before this project, so you may see code with questionable quality.

# Examples
```
.\AutoUnpack.exe json -i D:\Games\CalabiYau\CalabiyauGame\ -o json_export PaperMan/CSV PM/Content/PaperMan PaperMan/CyTable PM/Content/PaperMan WwiseAssets/AkEvent PM/Content/WwiseAssets
.\AutoUnpack.exe png -i D:\Games\CalabiYau\CalabiyauGame\ -o png_export DynamicResource/Emote PM/Content/PaperMan/UI/Atlas/DynamicResource DynamicResource/Item/ItemIcon PM/Content/PaperMan/UI/Atlas/DynamicResource
.\AutoUnpack.exe bin -i D:\Games\CalabiYau\CalabiyauGame\ -o audio_export WwiseAudio/Windows PM/Content/WwiseAudio/Windows
```

# Explanation

The first argument specifies the export type. Only `json`, `png`, and binary files are supported.

The `-i` argument is the game directory where `pak` files can be found.

The `-o` argument is where exports will be stored.

The remaining positional arguments appear in pairs of `filter replace`. Only directories that contain the string in `filter` will be exported. In the export process, the part of the directory that contains the string in `replace` will be truncated to reduce the amount of nesting in the directory structure. 

# Audio processing

Exported audio files can be further processed by `audio.py` in the `audio` directory. Example:

```
python audio.py <path to Windows directory in audio export>
```

The program assumes that the `Windows` directory contains the `Chinese` and `Japanese` subdirectory. It'll then automatically generate `bank.xml` files and extract `wav` files. To extract `wav` files `vgmstream-cli` must be available in `PATH`.

The audio processing utility embeds [wwiser](https://github.com/bnnm/wwiser) for convenience's sake.