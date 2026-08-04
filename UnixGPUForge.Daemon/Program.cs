using UnixGPUForge.Daemon.Core.Interfaces;
using UnixGPUForge.Daemon.Core.Providers;
using UnixGPUForge.Daemon.Core.Services;
using System.Text.Json;
using UnixGPUForge.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<AutoProfileService>();
builder.Services.AddHostedService<FanControlService>();

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

app.MapGet("/api/profiles", () =>
{
    if (!File.Exists("profiles.json")) return Results.Ok(new List<GameProfile>());
    var json = File.ReadAllText("profiles.json");
    var profiles = JsonSerializer.Deserialize<List<GameProfile>>(json);
    return Results.Ok(profiles);
});

// --- POST: Сохранить профили ---
app.MapPost("/api/profiles", (List<GameProfile> profiles) =>
{
    var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText("profiles.json", json);
    return Results.Ok(new { success = true });
});

// --- GET: Получить кривую кулеров ---
app.MapGet("/api/fancurve", () =>
{
    if (!File.Exists("fancurve.json")) 
    {
        // Дефолтная кривая, если файла еще нет
        var defaultCurve = new List<FanCurvePoint>
        {
            new() { Temperature = 40, FanSpeedPercent = 0 },
            new() { Temperature = 60, FanSpeedPercent = 40 },
            new() { Temperature = 80, FanSpeedPercent = 80 },
            new() { Temperature = 90, FanSpeedPercent = 100 }
        };
        return Results.Ok(defaultCurve);
    }
    
    var json = File.ReadAllText("fancurve.json");
    var curve = JsonSerializer.Deserialize<List<FanCurvePoint>>(json);
    return Results.Ok(curve);
});

// --- POST: Сохранить кривую кулеров ---
app.MapPost("/api/fancurve", (List<FanCurvePoint> curve) =>
{
    // Обязательно сортируем по температуре перед сохранением, чтобы логика не сломалась
    var sortedCurve = curve.OrderBy(p => p.Temperature).ToList();
    var json = JsonSerializer.Serialize(sortedCurve, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText("fancurve.json", json);
    return Results.Ok(new { success = true });
});

// Запускаем сервер на 5000 порту
app.Run("http://localhost:5000");

// --- DTOs для запросов ---
record PowerLimitRequest(uint Watts);
record ClockLockRequest(uint MaxClock);