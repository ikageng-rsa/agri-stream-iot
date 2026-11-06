using System.Globalization;
using AgriStream.Data;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.EntityFrameworkCore;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var storageConnectionString = builder.Configuration.GetConnectionString("AzureStorage")
    ?? throw new InvalidOperationException("Connection string 'AzureStorage' not found.");

builder.Services.AddSingleton(new TableServiceClient(storageConnectionString));
builder.Services.AddSingleton(new BlobServiceClient(storageConnectionString));
builder.Services.AddSingleton(new QueueServiceClient(storageConnectionString));

var sqlConnectionString = builder.Configuration.GetConnectionString("AzureSql")
    ?? throw new InvalidOperationException("Connection string 'AzureSql' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(sqlConnectionString, sql => sql.EnableRetryOnFailure()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
