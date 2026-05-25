using PdfSharp.Fonts;

namespace CNCToolingDatabase.Helpers;

public static class PdfFontBootstrap
{
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized) return;
        if (OperatingSystem.IsWindows())
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        _initialized = true;
    }
}
