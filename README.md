# Examples
```
.\AutoUnpack.exe json -i D:\Games\CalabiYau\CalabiyauGame\ -o json_export PaperMan/CSV PM/Content/PaperMan PaperMan/CyTable PM/Content/PaperMan WwiseAssets/AkEvent PM/Content/WwiseAssets
.\AutoUnpack.exe png -i D:\Games\CalabiYau\CalabiyauGame\ -o png_export DynamicResource/Emote PM/Content/PaperMan/UI/Atlas/DynamicResource DynamicResource/Item/ItemIcon PM/Content/PaperMan/UI/Atlas/DynamicResource
.\AutoUnpack.exe audio -i D:\Games\CalabiYau\CalabiyauGame\ -o audio_export WwiseAudio/Windows PM/Content/WwiseAudio/Windows
```

# Explanation

The first argument specifies the export type. 

The `-i` argument is the game directory where `pak` files can be found.

The `-o` argument is where exports will be stored.

The remaining positional arguments appear in pairs of `filter replace`. Only directories that contain the string in `filter` will be exported. In the export process, the part of the directory that contains the string in `replace` will be truncated to reduce the amount of nesting in the directory structure. 

By default, the program does not overwrite images and audio if there is already a file with the same name in the export directory. Adding `--force` will forcefully overwrite existing files. This is useful when an image/audio file might be updated. 