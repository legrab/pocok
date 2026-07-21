// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Pocok.AppDefaults.Logging;
using Pocok.Modularity;
using Pocok.Readiness;
using Pocok.Showcase.Components;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Web.Components;
using Pocok.Showcase.Web.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
if (!int.TryParse(port, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPort) ||
    parsedPort is < 1 or > 65_535)
    throw new InvalidOperationException("PORT must be a valid TCP port.");
builder.WebHost.UseUrls($"http://0.0.0.0:{parsedPort}");

new LoggingDefaultsConfigurator().Configure(builder);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddOptions<ShowcaseOptions>()
    .Bind(builder.Configuration.GetSection(ShowcaseOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => options.RunTimeout > TimeSpan.Zero && options.RunTimeout <= TimeSpan.FromSeconds(30),
        "Showcase run timeout must be greater than zero and no more than 30 seconds.")
    .Validate(options => options.ScriptingClientExecutionLimit == 0
                         || (options.ScriptingClientExecutionWindow > TimeSpan.Zero
                             && options.ScriptingClientExecutionWindow <= TimeSpan.FromDays(1)),
        "Showcase scripting client execution window must be greater than zero and no more than one day when limiting is enabled.")
    .Validate(options => string.IsNullOrWhiteSpace(options.PublicRepositoryBaseUrl)
                         || (Uri.TryCreate(options.PublicRepositoryBaseUrl, UriKind.Absolute, out Uri? uri)
                             && uri.Scheme is "https" or "http"),
        "Showcase public repository base URL must be an absolute HTTP or HTTPS URL.")
    .Validate(options => Enum.IsDefined(options.InAppLogMinimumLevel),
        "Showcase in-app log minimum level must be valid.")
    .ValidateOnStart();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
CultureInfo[] supportedCultures = [CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("hu")];
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = [new CookieRequestCultureProvider()];
});

var pluginDirectory = Environment.GetEnvironmentVariable("SHOWCASE_PLUGIN_DIR")
                      ?? Path.Combine(builder.Environment.ContentRootPath, "plugins");
builder.Services.AddPocokModules(builder.Configuration, options =>
{
    options
        .AddDirectory(pluginDirectory)
        .ShareAssemblyContaining<IShowcaseSlice>()
        .ShareAssemblyContaining<ShowcasePackageHeader>();
    options.IgnoreMissingDirectories = true;
    options.SearchRecursively = true;
    options.ThrowOnOptionalFailure = false;
});

builder.Services.AddSingleton(new ShowcaseResourceRegistration(
    "shell",
    builder.Environment.ContentRootPath,
    "Content/Locales/Shell"));
builder.Services.AddSingleton<ShowcasePackageCatalog>();
builder.Services.AddSingleton<ShowcaseTextCatalog>();
builder.Services.AddSingleton<IShowcaseText>(static provider => provider.GetRequiredService<ShowcaseTextCatalog>());
builder.Services.AddSingleton<ShowcaseSliceCatalog>();
builder.Services.AddSingleton<ReadinessSource>();
builder.Services.AddSingleton<IReadinessSignal>(static provider => provider.GetRequiredService<ReadinessSource>());
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ShowcaseRuntimeInfo>();
builder.Services.AddScoped<ShowcaseUiState>();
builder.Services.AddSingleton<ShowcasePublicLog>();
ShowcaseInAppLogging.Add(builder.Services, builder.Configuration);
builder.Services.AddSingleton<ShowcaseRunBuffer>();
builder.Services.AddSingleton<ShowcaseRunnerState>();
builder.Services.AddSingleton<ShowcaseScriptingClientLimiter>();
builder.Services.AddScoped<ShowcaseClientIdentity>();
builder.Services.AddScoped<ShowcaseRunClient>();
builder.Services.AddScoped<IShowcaseRunClient, RateLimitedShowcaseRunClient>();
builder.Services.AddHostedService<ShowcaseRunnerService>();
builder.Services.AddHostedService<ShowcaseStartupService>();

WebApplication app = builder.Build();
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseAntiforgery();
app.MapStaticAssets();
app.MapGet("/culture/set", (HttpContext context, string culture, string? returnUrl) =>
{
    if (!supportedCultures.Any(item => string.Equals(item.Name, culture, StringComparison.OrdinalIgnoreCase)))
        return Results.BadRequest("Unsupported culture.");
    var redirect = string.IsNullOrWhiteSpace(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
                                                        || returnUrl.StartsWith("//", StringComparison.Ordinal)
        ? "/"
        : returnUrl;
    context.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions
        {
            Path = "/",
            IsEssential = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps
        });
    return Results.LocalRedirect(redirect);
});
app.MapGet("/health/live", static () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", (ReadinessSource readiness) =>
    readiness.Snapshot.IsReady
        ? Results.Ok(new { status = "ready", sequence = readiness.Snapshot.Sequence })
        : Results.Json(new { status = readiness.State.ToString(), failure = readiness.Failure?.Code },
            statusCode: StatusCodes.Status503ServiceUnavailable));
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

public partial class Program;
