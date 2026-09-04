using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PolylineEncoder.Net.Utility;
using RailwaysManagement.Components;
using RailwaysManagement.Components.Account;
using RailwaysManagement.DbModels;
using RailwaysManagement.Extensions;
using RailwaysManagement.Models;
using RailwaysManagement.Models.Actions;
using RailwaysManagement.Services;
using Syncfusion.Blazor;
using System.Text.Json;
using System.Text.Json.Serialization;
using RailwaysManagementUser = RailwaysManagement.DbModels.RailwaysManagementUser;

var builder = WebApplication.CreateBuilder(args);

// set culture to en-GB
var cultureInfo = new System.Globalization.CultureInfo("en-GB");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<OpenRailRoutingService>();
builder.Services.AddSingleton<RoutingService>();
builder.Services.AddSingleton<RouteReportGenerationService>();
builder.Services.AddSingleton<GeoJsonService>();
builder.Services.AddSingleton<PolylineUtility>();
builder.Services.AddMemoryCache();

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NNaF1cWWhPYVFyWmFZfVtgd19CYVZTRmY/P1ZhSXxWdkNiXn5bdHVXRGFUWE19XUs= ");

builder.Services.AddSyncfusionBlazor();

// Configure System.Text.Json
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.Converters.Add(new RouteActionJsonConverter());
    options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Configure Newtonsoft.Json
builder.Services.Configure<Newtonsoft.Json.JsonSerializerSettings>(options =>
{
    options.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    options.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    options.Converters.Add(new NewtonsoftRouteActionConverter());
});

builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddDbContextFactory<RailwaysDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 40)) // Replace with your MySQL server version
    ).EnableSensitiveDataLogging(true));

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IdentityUserAccessor>();

builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();


builder.Services.AddDefaultIdentity<RailwaysManagementUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Disable email confirmation for simplicity
        options.User.RequireUniqueEmail = true; // Ensure unique email addresses
        options.Password.RequireNonAlphanumeric = false; // Allow simple passwords for testing
        options.Password.RequiredLength = 5; // Minimum password length
        options.Password.RequireDigit = false; // No digit requirement
        options.Password.RequireLowercase = false; // No lowercase requirement
        options.Password.RequireUppercase = false; // No uppercase requirement
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<RailwaysDbContext>()
    .AddSignInManager()
    .AddRoleManager<RoleManager<IdentityRole>>()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<RailwaysManagementUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting().
    UseAuthorization().
    UseAntiforgery().
    UseEndpoints(
    endpoints =>
    {
        endpoints.MapAccountServices();
    }
);
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();;

app.Run();