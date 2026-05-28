namespace CNCToolingDatabase.Helpers;

public static class PdfPreviewHelper
{
    public const string SampleStampFileName = "sample_stamp.PNG";
    public const string SamplePartImageFileName = "SAMPLE.jpg";
    public const string ToolSpecsFileName = "TOOL_SPECS.png";

    public static string GetSampleStampPath() =>
        DataFileResolver.Resolve("Data", "STAMP", SampleStampFileName);

    public static string GetSamplePartImagePath() =>
        DataFileResolver.Resolve("Data", "PART_IMAGE_SEED", SamplePartImageFileName);

    public static string GetToolSpecsPath() =>
        DataFileResolver.Resolve("Data", "PDF_EXPORT", ToolSpecsFileName);

    public static string GetSampleToolListPath() =>
        DataFileResolver.Resolve("Data", ToolListSampleLoader.SampleFileName);

    public static byte[]? LoadSampleStampBytes()
    {
        var path = GetSampleStampPath();
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    public static string? GetExistingSamplePartImagePath() =>
        File.Exists(GetSamplePartImagePath()) ? GetSamplePartImagePath() : null;

    public static string? GetExistingToolSpecsPath() =>
        File.Exists(GetToolSpecsPath()) ? GetToolSpecsPath() : null;
}
