using System.Runtime.InteropServices;
using SherpaOnnx;

namespace SpellingBee.Infrastructure.Sherpa;

/// <summary>
/// Tells sherpa-onnx where its native library is when it isn't a plain
/// <c>sherpa-onnx-c-api</c> file next to the app. On the iPad it ships inside
/// <c>SherpaOnnxC.framework</c>; the Mac needs no call because NuGet puts the library in place.
/// </summary>
public static class SherpaNativeLibrary
{
    private const string LibraryNameUsedBySherpa = "sherpa-onnx-c-api";

    public static void UseLibraryFile(string libraryPath)
    {
        NativeLibrary.SetDllImportResolver(typeof(OfflineRecognizer).Assembly, (name, _, _) =>
            name == LibraryNameUsedBySherpa ? NativeLibrary.Load(libraryPath) : IntPtr.Zero);
    }
}
