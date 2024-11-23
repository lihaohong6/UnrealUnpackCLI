﻿using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;

namespace AutoUnpack;

public class Utilities {
    private const string AesKey = "0x7456667CCC6BF87AAD3DAA2DDAC3B02564C5B9D74565BB36645C46AC210CDE40";

    public static DefaultFileProvider GetProvider(string providerRoot) {
        var provider = new DefaultFileProvider(providerRoot, SearchOption.AllDirectories,
            false, new VersionContainer(EGame.GAME_UE4_25));
        provider.Initialize(); // will scan the archive directory for supported file extensions
        provider.SubmitKey(new FGuid(), new FAesKey(AesKey));
        provider.Mount();
        return provider;
    }
}