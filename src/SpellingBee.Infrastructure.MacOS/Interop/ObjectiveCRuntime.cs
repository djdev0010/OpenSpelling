using System.Runtime.InteropServices;

namespace SpellingBee.Infrastructure.MacOS.Interop;

/// <summary>The few Objective-C runtime calls needed to ask macOS about the microphone.</summary>
internal static partial class ObjectiveCRuntime
{
    private const string Library = "/usr/lib/libobjc.A.dylib";

    [LibraryImport(Library, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr GetClass(string name);

    [LibraryImport(Library, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr GetSelector(string name);

    [LibraryImport(Library, EntryPoint = "objc_msgSend", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr SendWithString(IntPtr receiver, IntPtr selector, string argument);

    [LibraryImport(Library, EntryPoint = "objc_msgSend")]
    public static partial long SendWithPointerReturningLong(IntPtr receiver, IntPtr selector, IntPtr argument);

    public static IntPtr CreateNSString(string text) =>
        SendWithString(GetClass("NSString"), GetSelector("stringWithUTF8String:"), text);
}
