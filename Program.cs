using AIForged.API;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Configure AIForged settings and services
builder.Services.AddSingleton<Config>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var apiKey = configuration["AIForgedSettings:APIKey"];
    var baseUrl = configuration["AIForgedSettings:BaseUrl"];

    if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey))
    {
        throw new Exception("AIForged BaseUrl and APIKey must be configured in appsettings.json");
    }

    var config = new Config(baseUrl, "info");
    config.Init(allowAutoRedirect: true);
    config.HttpClient.DefaultRequestHeaders.Add("X-API-key", apiKey);

    return config;
});

builder.Services.AddSingleton<Context>(sp =>
{
    var config = sp.GetRequiredService<Config>();
    return new Context(config);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<Context>();
    try
    {
        var currentUser = await context.AccountClient.GetCurrentUserAsync();
        if (currentUser?.Result == null)
        {
            throw new Exception("Failed to authenticate with AIForged API");
        };

        Console.WriteLine($"Successfully authenticated as: {currentUser.Result.Email}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"AIForged authentication failed: {ex.Message}");
        throw;
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();