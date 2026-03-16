using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Supertoys.PocketSphinx;

public static class PocketSphinxRuntimePaths
{
    public static string GetModelsDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "models");
    }

    public static string GetNativeRuntimeDirectory()
    {
        var rid = GetDefaultRuntimeIdentifier();
        return Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native");
    }

    public static string GetPocketsphinxExecutablePath()
    {
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "pocketsphinx.exe"
            : "pocketsphinx";

        return Path.Combine(GetNativeRuntimeDirectory(), executableName);
    }

    public static string GetDefaultRuntimeIdentifier()
    {
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X64 => "x64",
            _ => "x64"
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return $"linux-{arch}";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return $"osx-{arch}";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"win-{arch}";
        }

        return $"linux-{arch}";
    }
}
