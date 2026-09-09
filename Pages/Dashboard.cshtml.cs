using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RegistrationApp.Core.Constants;
using RegistrationApp.Core.Configuration;
using RegistrationApp.Core.Time;
using RegistrationApp.Data;
using RegistrationApp.Models;
using RegistrationApp.Services;

namespace RegistrationApp.Pages;

public class DashboardModel : PageModel
{
    private const int PageSize = 5;

    private readonly ApplicationDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IConfiguration _configuration;

    public List<RegistrationDashboardDto> Registrations { get; set; } = new();
    public int TotalRegistrations { get; set; }
    public int ConfirmedRegistrations { get; set; }
    public int PendingPaymentRegistrations { get; set; }
    public int FailedPaymentRegistrations { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public DashboardModel(ApplicationDbContext dbContext, IBlobStorageService blobStorageService, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
        _configuration = configuration;
    }

    public async Task OnGetAsync([FromQuery] int page = 1)
    {
        CurrentPage = page < 1 ? 1 : page;

        // Calculate summary statistics
        TotalRegistrations = await _dbContext.Registrations.CountAsync();
        ConfirmedRegistrations = await _dbContext.Registrations
            .CountAsync(r => r.Status == RegistrationStatus.Confirmed);
        PendingPaymentRegistrations = await _dbContext.Registrations
            .CountAsync(r => r.Status == RegistrationStatus.PaymentPending);
        FailedPaymentRegistrations = await _dbContext.Registrations
            .CountAsync(r => r.Status == RegistrationStatus.PaymentFailed);

        TotalPages = (int)Math.Ceiling(TotalRegistrations / (double)PageSize);
        if (TotalPages > 0 && CurrentPage > TotalPages)
        {
            CurrentPage = TotalPages;
        }

        var registrations = await _dbContext.Registrations
            .Include(r => r.Payments)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        Registrations = registrations.Select(r => new RegistrationDashboardDto
        {
            Id = r.Id,
            Name = r.Name,
            PhoneNumber = r.PhoneNumber,
            Status = r.Status.ToString(),
            PaymentStatus = r.Payments != null && r.Payments.Any()
                ? r.Payments.OrderByDescending(p => p.CreatedAt).First().Status.ToString()
                : "None"
        }).ToList();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var registrations = await _dbContext.Registrations
            .Include(r => r.Category)
            .Include(r => r.Payments)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Registrations");

        var headers = new[]
        {
            "Registration ID", "Name", "Address", "Phone Number", "Taluka",
            "T-Shirt Size", "Category", "Registration Status", "Created At (UTC)",
            "Updated At (UTC)", "Confirmed At (UTC)", "Photo Link", "Aadhar Front Link",
            "Aadhar Back Link", "Payment Status", "Amount (INR)", "Currency",
            "Razorpay Order ID", "Razorpay Payment ID", "Payment Error"
        };

        for (var col = 0; col < headers.Length; col++)
        {
            worksheet.Cell(1, col + 1).Value = headers[col];
        }

        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#d97706");
        headerRow.Style.Font.FontColor = XLColor.White;

        var row = 2;
        foreach (var r in registrations)
        {
            var latestPayment = r.Payments != null && r.Payments.Any()
                ? r.Payments.OrderByDescending(p => p.CreatedAt).First()
                : null;

            worksheet.Cell(row, 1).Value = r.Id;
            worksheet.Cell(row, 2).Value = r.Name;
            worksheet.Cell(row, 3).Value = r.Address;
            worksheet.Cell(row, 4).Value = r.PhoneNumber;
            worksheet.Cell(row, 5).Value = r.Taluka;
            worksheet.Cell(row, 6).Value = r.TShirtSize;
            worksheet.Cell(row, 7).Value = r.Category?.Name ?? "Unknown";
            worksheet.Cell(row, 8).Value = r.Status.ToString();
            worksheet.Cell(row, 9).Value = r.CreatedAt;
            worksheet.Cell(row, 10).Value = r.UpdatedAt;
            worksheet.Cell(row, 11).Value = r.ConfirmedAt;
            SetImageLinkCell(worksheet.Cell(row, 12), r.PhotoBlobName);
            SetImageLinkCell(worksheet.Cell(row, 13), r.AadharFrontBlobName);
            SetImageLinkCell(worksheet.Cell(row, 14), r.AadharBackBlobName);
            worksheet.Cell(row, 15).Value = latestPayment?.Status.ToString() ?? "None";
            worksheet.Cell(row, 16).Value = latestPayment != null ? latestPayment.AmountInPaise / 100 : (decimal?)null;
            worksheet.Cell(row, 17).Value = latestPayment?.Currency;
            worksheet.Cell(row, 18).Value = latestPayment?.RazorpayOrderId;
            worksheet.Cell(row, 19).Value = latestPayment?.RazorpayPaymentId;
            worksheet.Cell(row, 20).Value = latestPayment?.ErrorMessage;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        var fileName = $"Registrations_{DateTimeProvider.IstNow:yyyyMMdd_HHmmss}.xlsx";
        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    /// <summary>
    /// Writes a clickable, time-limited download link for a stored image into the cell.
    /// Shows a placeholder when no image blob exists.
    /// </summary>
    private void SetImageLinkCell(IXLCell cell, string? blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            cell.Value = "N/A";
            return;
        }

        var expiryDays = _configuration.GetValue<int?>(
            $"{AzureStorageSettings.SectionName}:{nameof(AzureStorageSettings.ExportImageLinkExpiryDays)}")
            ?? ApplicationConstants.ExportImageLinkLifetime.Days;

        var lifetime = TimeSpan.FromDays(expiryDays > 0 ? expiryDays : 1);

        var url = _blobStorageService.GetDownloadUrl(blobName, lifetime);
        cell.Value = "Download";
        cell.SetHyperlink(new XLHyperlink(url));
        cell.Style.Font.FontColor = XLColor.Blue;
        cell.Style.Font.Underline = XLFontUnderlineValues.Single;
    }
}

public class RegistrationDashboardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
}