using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using DiffDemo.Data;
using DiffDemo.Models;
using DiffDemo.Services;
using MongoDB.Driver;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB
var mongoDbSettings = builder.Configuration.GetSection("MongoDB").Get<MongoDbSettings>();
if (mongoDbSettings == null)
{
    throw new InvalidOperationException("MongoDB configuration not found in appsettings.json");
}

builder.Services.AddSingleton(mongoDbSettings);
builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoDbSettings.ConnectionString));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddScoped<IMongoDbService, MongoDbService>();
builder.Services.AddScoped<Radzen.DialogService>();
builder.Services.AddScoped<Radzen.NotificationService>();
builder.Services.AddScoped<Radzen.TooltipService>();
builder.Services.AddScoped<Radzen.ContextMenuService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpClient>(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var httpContext = httpContextAccessor.HttpContext;
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    
    HttpClientHandler handler = new HttpClientHandler();
    
    // Skip SSL certificate validation in development
    if (environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = 
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    
    if (httpContext != null)
    {
        var request = httpContext.Request;
        var baseUri = $"{request.Scheme}://{request.Host}{request.PathBase}";
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseUri) };
        return httpClient;
    }
    
    // Fallback for when HttpContext is not available
    return new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5054") };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
