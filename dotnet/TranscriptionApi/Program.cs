using TranscriptionApi.Middleware;
using TranscriptionApi.Models;
using TranscriptionApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var startupLogger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Startup");
startupLogger.LogInformation("Starting ASP.NET Core Transcription API...");

// Load and validate configuration
var config = builder.Configuration
    .GetSection("Transcription")
    .Get<AppConfiguration>() ?? new AppConfiguration();

startupLogger.LogInformation("Configuration loaded - ModelSize: {ModelSize}, MaxWorkers: {MaxWorkers}, MaxQueueSize: {MaxQueueSize}, MaxFileSizeMB: {MaxFileSizeMB}",
    config.WhisperModelSize,
    config.MaxConcurrentWorkers,
    config.MaxQueueSize,
    config.MaxFileSizeMB);

AppConfigurationValidator.Validate(config);
startupLogger.LogInformation("Configuration validation passed");
builder.Services.AddSingleton(config);

// Register services
startupLogger.LogInformation("Registering services...");
builder.Services.AddSingleton<IWhisperModelService, WhisperModelService>();
builder.Services.AddSingleton<IJobManager, JobManager>();
builder.Services.AddScoped<ITranscriptionService, TranscriptionService>();
builder.Services.AddScoped<IAudioProcessor, AudioProcessor>();
startupLogger.LogInformation("Services registered successfully");

// Add controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add logging
builder.Services.AddLogging();

// Add OpenAPI for development (removed - using Swagger instead)

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application built successfully");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    logger.LogInformation("Running in Development environment");
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Transcription API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root (http://localhost:5000/)
        options.DocumentTitle = "Transcription API - Swagger UI";
    });
}
else
{
    logger.LogInformation("Running in {Environment} environment", app.Environment.EnvironmentName);
}

// Add request/response logging middleware (before other middleware)
app.UseMiddleware<RequestResponseLoggingMiddleware>();
logger.LogInformation("Request/response logging middleware enabled");

// Enable WebSockets
app.UseWebSockets();
logger.LogInformation("WebSocket support enabled");

// Use exception handler
app.UseExceptionHandler();
logger.LogInformation("Global exception handler enabled");

// Map controllers
app.MapControllers();
logger.LogInformation("Controllers mapped");

// Optionally warm up the model on startup
logger.LogInformation("Starting background model preload...");
var modelService = app.Services.GetRequiredService<IWhisperModelService>();
_ = Task.Run(async () =>
{
    try
    {
        logger.LogInformation("Preloading Whisper model in background...");
        await modelService.LoadModelAsync();
        logger.LogInformation("Whisper model preloaded successfully");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to preload Whisper model on startup - model will be loaded on first use");
    }
});

logger.LogInformation("Application startup complete - ready to accept requests");
app.Run();
