using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RegistrationApp.Data;
using RegistrationApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // Require authenticated users for the dashboard page
    options.Conventions.AuthorizePage("/Dashboard");
});

// Cookie authentication for the admin dashboard
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(4);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Add Entity Framework Core with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add logging
builder.Logging.AddApplicationInsights();
builder.Logging.AddConsole();

// Add Azure Blob Storage
// Configurable via appsettings.json ("AzureStorage" section):
//   - ConnectionString : use for local dev (Azurite) or access-key/SAS based auth
//   - AccountName      : use for production with Managed Identity (leave ConnectionString empty)
//   - ContainerName    : the blob container to store uploads (auto-created on startup)
var blobConnectionString = builder.Configuration.GetConnectionString("AzureStorage")
    ?? builder.Configuration["AzureStorage:ConnectionString"];
var blobAccountName = builder.Configuration["AzureStorage:AccountName"];
var containerName = builder.Configuration["AzureStorage:ContainerName"] ?? "registrations";

BlobServiceClient blobServiceClient;

if (!string.IsNullOrWhiteSpace(blobConnectionString))
{
    // Connection string based (local dev / access key / SAS)
    blobServiceClient = new BlobServiceClient(blobConnectionString);
}
else if (!string.IsNullOrWhiteSpace(blobAccountName))
{
    // Managed Identity based (production)
    var blobServiceUri = new Uri($"https://{blobAccountName}.blob.core.windows.net");
    blobServiceClient = new BlobServiceClient(blobServiceUri, new DefaultAzureCredential());
}
else
{
    throw new InvalidOperationException(
        "Azure Storage is not configured. Set either 'AzureStorage:ConnectionString' " +
        "(local/dev or access key) or 'AzureStorage:AccountName' (production Managed Identity) in appsettings.");
}

var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

builder.Services.AddSingleton(blobContainerClient);

// Add custom services
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Add controllers for API endpoints (webhooks, etc.)
builder.Services.AddControllers();

// Add HttpClient for Razorpay API calls
builder.Services.AddHttpClient<PaymentService>();

var app = builder.Build();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();

    // Ensure the blob container exists (auto-create so it "just works" after configuring secrets)
    var containerClient = scope.ServiceProvider.GetRequiredService<BlobContainerClient>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await containerClient.CreateIfNotExistsAsync();
        logger.LogInformation("Blob container '{ContainerName}' is ready.", containerClient.Name);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ensure blob container '{ContainerName}' exists. " +
            "Verify AzureStorage settings in appsettings.", containerClient.Name);
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
