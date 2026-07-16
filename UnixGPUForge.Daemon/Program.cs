using UnixGPUForge.Daemon.Core.Interfaces;
using UnixGPUForge.Daemon.Core.Providers;
using UnixGPUForge.Daemon.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<AutoProfileService>();

// Разрешаем CORS, чтобы Vue-клиент (обычно висящий на localhost:5173) мог стучаться к демону
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Регистрируем наш NVML-провайдер как Singleton. 
// Инициализация драйвера пройдет один раз при старте демона.
builder.Services.AddSingleton<IGpuProvider, NvidiaGpuProvider>();

var app = builder.Build();

app.UseCors();

// --- GET: Метрики ---
app.MapGet("/api/gpu/metrics", (IGpuProvider gpu) =>
{
    return Results.Ok(gpu.GetTelemetry());
});

// --- POST: Лимит мощности ---
app.MapPost("/api/gpu/powerlimit", (IGpuProvider gpu, PowerLimitRequest request) =>
{
    bool success = gpu.SetPowerLimit(request.Watts);
    return success ? Results.Ok(new { success = true }) 
                   : Results.BadRequest(new { success = false, message = "Failed to set power limit." });
});

// --- POST: Частоты ядра ---
app.MapPost("/api/gpu/clocklock", (IGpuProvider gpu, ClockLockRequest request) => 
{
    // Безопасный минимум 200 МГц, максимум берем из запроса
    bool success = gpu.SetGpuLockedClocks(200, request.MaxClock);
    return success ? Results.Ok(new { success = true }) 
                   : Results.BadRequest(new { success = false, message = "Failed to lock core clock." });
});

app.MapPost("/api/gpu/clockreset", (IGpuProvider gpu) => 
{
    return gpu.ResetGpuLockedClocks() ? Results.Ok(new { success = true }) 
                                      : Results.BadRequest(new { success = false, message = "Failed to reset clocks." });
});

// --- POST: Частоты памяти ---
app.MapPost("/api/gpu/memclocklock", (IGpuProvider gpu, ClockLockRequest request) => 
{
    // Безопасный минимум 400 МГц для памяти
    bool success = gpu.SetMemoryLockedClocks(400, request.MaxClock);
    return success ? Results.Ok(new { success = true }) 
                   : Results.BadRequest(new { success = false, message = "Failed to lock memory clock." });
});

app.MapPost("/api/gpu/memclockreset", (IGpuProvider gpu) => 
{
    return gpu.ResetMemoryLockedClocks() ? Results.Ok(new { success = true }) 
                                         : Results.BadRequest(new { success = false, message = "Failed to reset memory clocks." });
});

// Запускаем сервер на 5000 порту
app.Run("http://localhost:5000");

// --- DTOs для запросов ---
record PowerLimitRequest(uint Watts);
record ClockLockRequest(uint MaxClock);