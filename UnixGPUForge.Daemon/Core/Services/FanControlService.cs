using System.Text.Json;
using UnixGPUForge.Daemon.Core.Interfaces;
using UnixGPUForge.Shared.Models;

namespace UnixGPUForge.Daemon.Core.Services;

public class FanControlService : BackgroundService
{
    private readonly IGpuProvider _gpu;
    private readonly ILogger<FanControlService> _logger;

    private uint _lastAppliedSpeed = 0;
    // Гистерезис: меняем скорость только если разница больше 3%
    private readonly uint _hysteresisThreshold = 3;

    // Базовая хардкод-кривая (позже вынесем её загрузку из JSON, как ты сделал с профилями)
    private List<FanCurvePoint> _globalCurve = new()
    {
        new FanCurvePoint { Temperature = 30, FanSpeedPercent = 0 },   // Zero RPM mode
        new FanCurvePoint { Temperature = 50, FanSpeedPercent = 30 },  // Легкий старт
        new FanCurvePoint { Temperature = 65, FanSpeedPercent = 50 },  // Средняя нагрузка
        new FanCurvePoint { Temperature = 75, FanSpeedPercent = 80 },  // Пошла жара
        new FanCurvePoint { Temperature = 85, FanSpeedPercent = 100 }  // Троттлинг близко
    };

    public FanControlService(IGpuProvider gpu, ILogger<FanControlService> logger)
    {
        _gpu = gpu;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[FAN] Служба управления вентиляторами запущена.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Загружаем кривую из файла на каждой итерации (или раз в N секунд)
            if (File.Exists("fancurve.json"))
            {
                var json = File.ReadAllText("fancurve.json");
                _globalCurve = JsonSerializer.Deserialize<List<FanCurvePoint>>(json) ?? _globalCurve;
            }

            try
            {
                var telemetry = _gpu.GetTelemetry();
                uint targetSpeed = CalculateFanSpeed(telemetry.Temperature, _globalCurve);

                // Защита от дребезга (гистерезис)
                if (Math.Abs((int)targetSpeed - (int)_lastAppliedSpeed) >= _hysteresisThreshold)
                {
                    // В NVML кулеры индексируются (обычно 0 и 1).
                    // Для надежности применяем к обоим (можно динамически получать их количество, но пока так)
                    _gpu.SetFanSpeed(0, targetSpeed);
                    _gpu.SetFanSpeed(1, targetSpeed);

                    _lastAppliedSpeed = targetSpeed;
                    _logger.LogDebug($"[FAN] Температура: {telemetry.Temperature}°C. Скорость: {targetSpeed}%");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[FAN] Ошибка поллинга кулеров: {ex.Message}");
            }

            // Поллинг каждую секунду для быстрой реакции на нагрев
            await Task.Delay(1000, stoppingToken);
        }
    }

    private uint CalculateFanSpeed(uint currentTemp, List<FanCurvePoint> curve)
    {
        var sortedCurve = curve.OrderBy(p => p.Temperature).ToList();

        // Ниже минимальной точки
        if (currentTemp <= sortedCurve.First().Temperature)
            return sortedCurve.First().FanSpeedPercent;

        // Выше максимальной точки
        if (currentTemp >= sortedCurve.Last().Temperature)
            return sortedCurve.Last().FanSpeedPercent;

        // Поиск интервала и линейная интерполяция
        for (int i = 0; i < sortedCurve.Count - 1; i++)
        {
            var p1 = sortedCurve[i];
            var p2 = sortedCurve[i + 1];

            if (currentTemp >= p1.Temperature && currentTemp <= p2.Temperature)
            {
                float tempRange = p2.Temperature - p1.Temperature;
                float speedRange = p2.FanSpeedPercent - p1.FanSpeedPercent;

                float progress = (currentTemp - p1.Temperature) / tempRange;
                return (uint)(p1.FanSpeedPercent + (speedRange * progress));
            }
        }

        return 50; // Fallback на 50% если что-то пошло не так
    }
}