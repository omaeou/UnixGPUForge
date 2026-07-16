using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using UnixGPUForge.Daemon.Core.Interfaces;

namespace UnixGPUForge.Daemon.Core.Services;

// Структура нашего профиля игры
public record GameProfile(string ProcessName, uint TargetPowerLimit, string DisplayName);

public class AutoProfileService : BackgroundService
{
    private readonly IGpuProvider _gpu;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pids = _gpu.GetRunningPids();
            foreach (var pid in pids)
            {
                string processName = GetProcessNameByPid(pid);
                if (IsGame(processName)) 
                {
                    ApplyGameProfile(processName);
                }
            }
            await Task.Delay(2000, stoppingToken); // Проверяем раз в 2 секунды
        }
    }
}