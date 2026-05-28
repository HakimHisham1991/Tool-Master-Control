using ClosedXML.Excel;
using CNCToolingDatabase.Models.ViewModels;

namespace CNCToolingDatabase.Helpers;

public static class ToolListSampleLoader
{
    public const string SampleFileName = "TOOL_LIST_SAMPLE.xlsx";

    public static (ToolListEditorViewModel ViewModel, List<ToolListDetailRow> Details) Load()
    {
        var path = ResolveSamplePath();
        if (!File.Exists(path))
            return (CreateFallbackViewModel(), CreateFallbackDetails());

        using var workbook = new XLWorkbook(path);
        var ws = workbook.Worksheets.FirstOrDefault();
        if (ws == null || ExcelHelper.GetUsedRowCount(ws) == 0)
            return (CreateFallbackViewModel(), CreateFallbackDetails());

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int rows = ExcelHelper.GetUsedRowCount(ws);
        int headerRow = -1;

        for (int r = 1; r <= rows; r++)
        {
            var label = ExcelHelper.GetString(ws, r, 1).TrimEnd(':').Trim();
            var value = ExcelHelper.GetString(ws, r, 2);

            if (string.Equals(label, "Tool No.", StringComparison.OrdinalIgnoreCase))
            {
                headerRow = r;
                break;
            }

            if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                fields[label] = value;
        }

        var viewModel = new ToolListEditorViewModel
        {
            ToolListName = GetField(fields, "Tool List"),
            PartNumber = GetField(fields, "Part Number"),
            PartDescription = GetField(fields, "Part Description"),
            Operation = GetField(fields, "Operation"),
            Revision = GetField(fields, "Revision"),
            ProjectCode = GetField(fields, "Project Code"),
            MachineName = GetField(fields, "Machine"),
            MachineWorkcenter = GetField(fields, "Workcenter"),
            MachineModel = GetField(fields, "Machine Model"),
            CamProgrammer = GetField(fields, "CAM Programmer"),
            ApprovedBy = GetField(fields, "Approved By"),
            ToolRegisterByName = GetField(fields, "Tool Register By"),
            ApprovedDate = DateTime.Today.AddDays(-3),
            CamLeaderApprovedDate = DateTime.Today.AddDays(-2),
            ToolRegisterByDate = DateTime.Today.AddDays(-1)
        };

        var details = headerRow > 0
            ? LoadDetails(ws, headerRow, rows)
            : CreateFallbackDetails();

        for (int r = headerRow > 0 ? headerRow + 1 : 1; r <= rows; r++)
        {
            var label = ExcelHelper.GetString(ws, r, 1).TrimEnd(':').Trim();
            var value = ExcelHelper.GetString(ws, r, 2);
            if (string.IsNullOrWhiteSpace(label)) continue;

            if (string.Equals(label, "CAM Programmer", StringComparison.OrdinalIgnoreCase))
                viewModel.CamProgrammer = value;
            else if (string.Equals(label, "Approved By", StringComparison.OrdinalIgnoreCase))
                viewModel.ApprovedBy = value;
            else if (string.Equals(label, "Tool Register By", StringComparison.OrdinalIgnoreCase))
                viewModel.ToolRegisterByName = value;
        }

        return (viewModel, details);
    }

    private static List<ToolListDetailRow> LoadDetails(IXLWorksheet ws, int headerRow, int rows)
    {
        int cols = Math.Max(ExcelHelper.GetUsedColumnCount(ws), 12);
        int colToolNo = ExcelHelper.GetColumn(ws, headerRow, cols, "Tool No.");
        int colToolName = ExcelHelper.GetColumn(ws, headerRow, cols, "Tool Name");
        int colConsumable = ExcelHelper.GetColumn(ws, headerRow, cols, "Consumable Tool Description");
        int colSupplier = ExcelHelper.GetColumn(ws, headerRow, cols, "Tool Supplier");
        int colHolder = ExcelHelper.GetColumn(ws, headerRow, cols, "Tool Holder");
        int colDiameter = ExcelHelper.GetColumn(ws, headerRow, cols, "Tool Diameter (D1)", "Diameter");
        int colFluteLength = ExcelHelper.GetColumn(ws, headerRow, cols, "Flute Length (L1)");
        int colExtLength = ExcelHelper.GetColumn(ws, headerRow, cols, "Tool Ext. Length (L2)");
        int colCornerRadius = ExcelHelper.GetColumn(ws, headerRow, cols, "Tool Corner Radius");
        int colArbor = ExcelHelper.GetColumn(ws, headerRow, cols, "Arbor Description (or equivalent specs)", "Arbor Description");
        int colToolPathTime = ExcelHelper.GetColumn(ws, headerRow, cols, "Tool Path Time in Minutes");
        int colRemarks = ExcelHelper.GetColumn(ws, headerRow, cols, "Remarks");

        var details = new List<ToolListDetailRow>();
        for (int r = headerRow + 1; r <= rows; r++)
        {
            var firstCol = ExcelHelper.GetString(ws, r, 1);
            if (firstCol.EndsWith(':'))
                break;

            var toolNumber = ExcelHelper.GetString(ws, r, colToolNo);
            var consumable = ExcelHelper.GetString(ws, r, colConsumable);
            if (string.IsNullOrWhiteSpace(toolNumber) && string.IsNullOrWhiteSpace(consumable))
                continue;

            details.Add(new ToolListDetailRow
            {
                ToolNumber = toolNumber,
                ToolDescription = ExcelHelper.GetString(ws, r, colToolName),
                ConsumableCode = consumable,
                Supplier = ExcelHelper.GetString(ws, r, colSupplier),
                HolderExtensionCode = ExcelHelper.GetString(ws, r, colHolder),
                Diameter = ExcelHelper.ParseDecimal(ws, r, colDiameter),
                FluteLength = ExcelHelper.ParseDecimal(ws, r, colFluteLength),
                ProtrusionLength = ExcelHelper.ParseDecimal(ws, r, colExtLength),
                CornerRadius = ExcelHelper.ParseDecimal(ws, r, colCornerRadius),
                ArborCode = ExcelHelper.GetString(ws, r, colArbor),
                ToolPathTimeMinutes = ExcelHelper.ParseDecimal(ws, r, colToolPathTime),
                Remarks = ExcelHelper.GetString(ws, r, colRemarks)
            });
        }

        return details;
    }

    private static string GetField(Dictionary<string, string> fields, string label) =>
        fields.TryGetValue(label, out var value) ? value : string.Empty;

    private static string ResolveSamplePath() =>
        PdfPreviewHelper.GetSampleToolListPath();

    private static ToolListEditorViewModel CreateFallbackViewModel() => new()
    {
        ToolListName = "SAMPLE_TOOL_LIST",
        PartNumber = "SAMPLE",
        PartDescription = "Sample Part",
        Operation = "OP10",
        Revision = "REV00",
        ProjectCode = "PRJ-001",
        MachineName = "Sample Machine",
        MachineWorkcenter = "WC-01",
        MachineModel = "Model X",
        CamProgrammer = "John Doe",
        ApprovedBy = "Jane Doe",
        ToolRegisterByName = "Admin",
        ApprovedDate = DateTime.Today.AddDays(-3),
        CamLeaderApprovedDate = DateTime.Today.AddDays(-2),
        ToolRegisterByDate = DateTime.Today.AddDays(-1)
    };

    private static List<ToolListDetailRow> CreateFallbackDetails() => new()
    {
        new()
        {
            ToolNumber = "T01", ToolDescription = "Sample Tool", ConsumableCode = "CODE 001",
            Supplier = "Sample Supplier", HolderExtensionCode = "HOLDER-01",
            Diameter = 10, FluteLength = 25, ProtrusionLength = 45, CornerRadius = 0,
            ArborCode = "ARBOR-01", ToolPathTimeMinutes = 1.5m, Remarks = "Sample"
        }
    };
}
