using System.Text.Json;
using UnixGPUForge.Daemon.Core.Interfaces;
using UnixGPUForge.Shared.Models;

namespace UnixGPUForge.Daemon.Core.Services;

public class FanControlService : BackgroundService
{
    private readonly IGpuProvider _gpu;
    private readonly ILogger<FanControlService> _logger;

    private uint _lastAppliedSpeed = 0;
    private readonly uint _hysteresisThreshold = 3;
    private List<FanCurvePoint> _globalCurve = new()
    {
        new FanCurvePoint { Temperature = 30, FanSpeedPercent = 0 }, 
        new FanCurvePoint { Temperature = 50, FanSpeedPercent = 30 },
        new FanCurvePoint { Temperature = 65, FanSpeedPercent = 50 },
        new FanCurvePoint { Temperature = 75, FanSpeedPercent = 80 },
        new FanCurvePoint { Temperature = 85, FanSpeedPercent = 100 }
    };

    public FanControlService(IGpuProvider gpu, ILogger<FanControlService> logger)
    {
        _gpu = gpu;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1000, stoppingToken);

        uint fanCount = 1; 
        try
        {
            fanCount = _gpu.GetFanCount();
            _logger.LogInformation($"FanControlService initialized. Detected {fanCount} cooling fan(s).");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch fan count dynamically. Defaulting to 1 fan.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var telemetry = _gpu.GetTelemetry();
                
                uint targetSpeed = CalculateFanSpeed(telemetry.Temperature, _globalCurve);

                if (Math.Abs((int)targetSpeed - (int)_lastAppliedSpeed) >= _hysteresisThreshold)
                {
                    bool allSuccess = true;
                    
                    for (uint i = 0; i < fanCount; i++)
                    {
                        if (!_gpu.SetFanSpeed(i, targetSpeed))
                        {
                            allSuccess = false;
                        }
                    }

                    // Если драйвер принял команду, сохраняем стейт
                    if (allSuccess)
                    {
                        _logger.LogInformation($"Fan speed successfully updated to {targetSpeed}% (Temp: {telemetry.Temperature}°C)");
                        _lastAppliedSpeed = targetSpeed;
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to apply {targetSpeed}%. Driver rejected the NVML command.");
                        // Не обновляем _lastAppliedSpeed, чтобы сервис попытался снова на следующей итерации
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying dynamic fan curve.");
            }

            await Task.Delay(2000, stoppingToken);
        }
    }

    private uint CalculateFanSpeed(uint currentTemp, List<FanCurvePoint> curve)
    {
        var sortedCurve = curve.OrderBy(p => p.Temperature).ToList();

        if (currentTemp <= sortedCurve.First().Temperature)
            return sortedCurve.First().FanSpeedPercent;

        if (currentTemp >= sortedCurve.Last().Temperature)
            return sortedCurve.Last().FanSpeedPercent;

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

        return 50; 
    }
}