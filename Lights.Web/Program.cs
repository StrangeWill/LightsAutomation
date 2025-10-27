using Lights.Web.AddHostedService;
using Lights.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.local.json", true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var configuration = builder.Configuration;

builder.Services
    .Configure<ForwardedHeadersOptions>(options =>
    {
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    })
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles)
    .Services
    .AddSwaggerGen()
    .AddSingleton<LightService>()
    .AddSingleton<FileService>()
    .AddHostedService(provider => provider.GetRequiredService<LightService>());

var routes = string.Join('|', new[] { "" });
var options = new RewriteOptions()
    .AddRewrite($"^({routes}).*", "index.html", skipRemainingRules: true);

var app = builder.Build();
app
    .UseForwardedHeaders()
    .UseDefaultFiles()
    .UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = (context) =>
        {
            if (context.File.Name.EndsWith(".json") ||
                context.File.Name.EndsWith(".html"))
            {
                context.Context.Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
                {
                    Public = true,
                    MaxAge = TimeSpan.FromDays(0)
                };
            }
        }
    })
    .UseSwagger()
    .UseSwaggerUI()
    //.UseRewriter(options)
    .UseRouting()
    .UseCors()
    .UseAuthentication()
    .UseAuthorization()
    .UseRouting()
    .UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapSwagger();
    });

await app.RunAsync();