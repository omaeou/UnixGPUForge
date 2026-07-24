using System.Diagnostics;
using Microsoft.Extensions.Hosting;  // Для BackgroundService
using Microsoft.Extensions.Logging;  // Для ILogger
using System.Text.Json;
using UnixGPUForge.Daemon.Core.Interfaces;
using UnixGPUForge.Shared.Models;

namespace UnixGPUForge.Daemon.Core.Services;


public class AutoProfileService : BackgroundService
{
    private readonly IGpuProvider _gpu;
    private readonly ILogger<AutoProfileService> _logger;
    private List<GameProfile> _profiles = new();
    private string? _activeProfileName = null;
    private static readonly object _nvmlLock = new();
    public AutoProfileService(IGpuProvider gpu, ILogger<AutoProfileService> logger)
    {
        _gpu = gpu;
        _logger = logger;
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        try
        {
            if (File.Exists("profiles.json"))
            {
                string json = File.ReadAllText("profiles.json");
                _profiles = JsonSerializer.Deserialize<List<GameProfile>>(json) ?? new List<GameProfile>();
                _logger.LogInformation($"Загружено {_profiles.Count} профилей автоматизации.");
            }
            else
            {
                // Генерируем дефолтный шаблон
                _profiles = new List<GameProfile>
                {
                    new() { ProcessName = "Cyberpunk2077", PowerLimit = 300, CoreClock = 2650, MemoryClock = 11200 },
                    new() { ProcessName = "dota2", PowerLimit = 150, CoreClock = 1800, MemoryClock = 10000 },
                    new() { ProcessName = "HuntGame", PowerLimit = 280, CoreClock = 2500, MemoryClock = 11200 },
                    new() { ProcessName = "bg3", PowerLimit = 200, CoreClock = 2000, MemoryClock = 10500 }
                };
                File.WriteAllText("profiles.json", JsonSerializer.Serialize(_profiles, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Ошибка загрузки profiles.json: {ex.Message}");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                LoadProfiles();
                var pids = _gpu.GetRunningPids();
                GameProfile? matchedProfile = null;

                foreach (var pid in pids)
                {
                    try
                    {
                        var process = Process.GetProcessById((int)pid);
                        string procName = process.ProcessName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
                        
                        // Ищем игру в списке (независимо от регистра)
                        matchedProfile = _profiles.FirstOrDefault(p => 
                            procName.Contains(p.ProcessName.Replace(".exe", ""), StringComparison.OrdinalIgnoreCase));
                        
                        if (matchedProfile != null) break;
                    }
                    catch
                    {
                        // Процесс мог уже завершиться, игнорируем
                    }
                }

                // Применяем профиль, если игра найдена и мы еще не в этом профиле
                if (matchedProfile != null && _activeProfileName != matchedProfile.ProcessName)
                {
                    _logger.LogInformation($"[AUTO] Обнаружена игра: {matchedProfile.ProcessName}. Применяем настройки железа...");
                    lock (_nvmlLock)
                    {
                    _gpu.SetPowerLimit(matchedProfile.PowerLimit);
                    _gpu.SetGpuLockedClocks(200, matchedProfile.CoreClock);
                    _gpu.SetMemoryLockedClocks(400, matchedProfile.MemoryClock);
                    }
                    _activeProfileName = matchedProfile.ProcessName;
                }
                // Если игра закрыта, сбрасываем всё на дефолт
                else if (matchedProfile == null && _activeProfileName != null)
                {
                    _logger.LogInformation("[AUTO] Игра закрыта. Возврат к дефолтным частотам...");
                    _gpu.ResetGpuLockedClocks();
                    _gpu.ResetMemoryLockedClocks();
                    
                    _activeProfileName = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка мониторинга процессов: {ex.Message}");
            }

            // Ждем 3 секунды до следующей проверки
            await Task.Delay(5000, stoppingToken);
        }
    }
}