using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http; // <-- Добавлено для работы Results.Ok и Results.BadRequest
using UnixGPUForge.Daemon.Core.Interfaces;
using UnixGPUForge.Daemon.Core.Providers;
using UnixGPUForge.Daemon.Core.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// Регистрируем нашу видеокарту как Singleton
builder.Services.AddSingleton<IGpuProvider, NvidiaGpuProvider>();

builder.Services.AddHostedService<AutoProfileService>();

// Настраиваем CORS для Vue
builder.Services.AddCors(options => {
    options.AddPolicy("AllowVueFront", policy => {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors("AllowVueFront");

// GET-эндпоинт: получение метрик
app.MapGet("/api/gpu/metrics", (IGpuProvider gpu) => 
{
    var mem = gpu.GetMemoryInfo();
    var util = gpu.GetUtilization();
    
    return new {
        Name = gpu.GetDeviceName(),
        Temperature = gpu.GetCoreTemperature(),
        PowerUsage = gpu.GetPowerUsage(),
        powerLimitMax = gpu.GetPowerLimitConstraints().Max,
        powerLimitMin = gpu.GetPowerLimitConstraints().Min,
        CoreLoad = util.Gpu,
        MemLoad = util.Memory,
        VramUsed = mem.Used,
        VramTotal = mem.Total
    };
});

app.MapGet("/api/gpu/powerlimit", (IGpuProvider gpu) => 
{
    var (min, max) = gpu.GetPowerLimitConstraints();
    return new { Min = min, Max = max };
});

// POST-эндпоинт: установка лимита потребления
app.MapPost("/api/gpu/powerlimit", (IGpuProvider gpu, PowerLimitRequest request) => 
{
    bool success = gpu.SetPowerLimit(request.Watts);
    
    if (success)
    {
        Console.WriteLine($"[INFO] Power Limit set to {request.Watts}W");
        return Results.Ok(new { success = true, message = $"Limit set to {request.Watts}W" });
    }
    
    return Results.BadRequest(new { success = false, message = "Failed to set Power Limit. Are you running as root?" });
});

app.MapPost("/api/gpu/clocklock", (IGpuProvider gpu, ClockLockRequest request) => 
{
    // Минимальную частоту ставим на 200 МГц (базовая безопасность драйвера), 
    // а максимальную фиксируем по ползунку
    bool success = gpu.SetGpuLockedClocks(200, request.MaxClock);
    
    if (success)
    {
        Console.WriteLine($"[INFO] Core Clock locked at {request.MaxClock} MHz");
        return Results.Ok(new { success = true });
    }
    return Results.BadRequest(new { success = false, message = "Failed to lock core clock." });
});

app.MapPost("/api/gpu/clockreset", (IGpuProvider gpu) => 
{
    bool success = gpu.ResetGpuLockedClocks();
    if (success) Console.WriteLine("[INFO] Core Clocks reset to default.");
    
    return success 
        ? Results.Ok(new { success = true }) 
        : Results.BadRequest(new { success = false, message = "Failed to reset clocks." });
});

Console.WriteLine("=== UnixGPUForge Daemon API Started ===");
Console.WriteLine("API is running on: http://localhost:5000/api/gpu/metrics");

// Запуск сервера
app.Run("http://localhost:5000");

// ==========================================
// Типы и структуры ВСЕГДА должны быть в самом низу файла
// ==========================================
record PowerLimitRequest(uint Watts);
record ClockLockRequest(uint MaxClock);

