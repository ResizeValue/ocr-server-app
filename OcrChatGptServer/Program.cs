using OcrChatGptServer.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add necessary services
builder.Services.AddControllers();

// 2. Register queue and background worker
builder.Services.AddSingleton<OcrChatGptQueue>();
builder.Services.AddHostedService<OcrChatGptQueueWorker>();

// 3. Register OCR and ChatGPT service
builder.Services.AddSingleton<GptService>();
builder.Services.AddSingleton<ImgurService>();
builder.Services.AddSingleton<IOcrService, AzureOcrService>();

builder.Services.AddOpenApi();

// 4. Build the app
var app = builder.Build();

app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi(); // Exposes OpenAPI spec at `/openapi/v1.json`
    app.UseSwaggerUi(options =>
    {
        options.DocumentPath = "openapi/v1.json";
    });
}

app.MapControllers();

app.Run();
