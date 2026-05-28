using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace CNCToolingDatabase.Helpers;

/// <summary>
/// MigraDoc/PDFsharp only embed PNG and JPEG. Converts other formats (e.g. GIF) before PDF export.
/// </summary>
public static class PdfImageHelper
{
    private static readonly HashSet<string> PdfSupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg"
    };

    public static string? PrepareImagePath(string? path, ICollection<string>? tempFiles = null)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        if (PdfSupportedExtensions.Contains(Path.GetExtension(path)))
            return path;

        return ConvertToTempPng(path, null, tempFiles);
    }

    public static string? PrepareImageBytes(byte[]? bytes, ICollection<string>? tempFiles = null)
    {
        if (bytes is not { Length: > 0 })
            return null;

        if (IsPng(bytes) || IsJpeg(bytes))
        {
            var ext = IsPng(bytes) ? ".png" : ".jpg";
            var path = CreateTempPath(ext);
            File.WriteAllBytes(path, bytes);
            tempFiles?.Add(path);
            return path;
        }

        return ConvertToTempPng(null, bytes, tempFiles);
    }

    private static string? ConvertToTempPng(string? sourcePath, byte[]? sourceBytes, ICollection<string>? tempFiles)
    {
        try
        {
            using var image = sourcePath != null
                ? Image.Load(sourcePath)
                : Image.Load(sourceBytes!);

            var path = CreateTempPath(".png");
            image.Save(path, new PngEncoder());
            tempFiles?.Add(path);
            return path;
        }
        catch
        {
            return null;
        }
    }

    private static string CreateTempPath(string ext) =>
        Path.Combine(Path.GetTempPath(), $"tooling-pdf-{Guid.NewGuid():N}{ext}");

    private static bool IsPng(byte[] bytes) =>
        bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;

    private static bool IsJpeg(byte[] bytes) =>
        bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
}
