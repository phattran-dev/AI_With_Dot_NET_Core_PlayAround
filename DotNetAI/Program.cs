using DotNetAI.Services;
using DotnetGeminiSDK;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddGeminiClient(config =>
{
    config.ApiKey = builder.Configuration.GetValue<string>("Gemini:ApiKey");
    config.ModelBaseUrl = builder.Configuration.GetValue<string>("Gemini:Url");
});
// Add services to the container.
builder.Services.AddScoped<IAzureDocumentIntelligenceService, AzureDocumentIntelligenceService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
