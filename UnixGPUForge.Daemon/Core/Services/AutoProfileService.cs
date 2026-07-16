using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using UnixGPUForge.Daemon.Core.Interfaces;

namespace UnixGPUForge.Daemon.Core.Services;

// Структура нашего профиля игры
public record GameProfile(string ProcessName, uint TargetPowerLimit, string DisplayName);

public class AutoProfileService : BackgroundService
{
    private readonly IGpuProvider _gpu;
    private readonly uint _desktopPowerLimit = 150; // Тихий режим для рабочего стола
    private string _currentActiveGame = string.Empty;

    // В будущем мы будем грузить это из JSON-файла, пока захардкодим для архитектуры
    private readonly List<GameProfile> _profiles = new()
    {
        new("hunt", 330, "Hunt: 1896"),            // Тяжелый шутер, отдаем всю мощность
        new("bg3", 220, "Baldur's Gate 3"),        // RPG, можно немного "придушить" карту
        new("dota2", 150, "Dota 2"),               // Киберспорт, карте напрягаться не нужно
        new("obs", 250, "OBS Studio (Streaming)")  // Стриминг, баланс для NVENC
    };

    public AutoProfileService(IGpuProvider gpu)
    {
        _gpu = gpu;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[AutoProfile] Service started. Scanning for games...");

        while (!stoppingToken.IsCancellationRequested)
        {
            CheckProcesses();
            await Task.Delay(3000, stoppingToken); // Сканируем каждые 3 секунды
        }
    }

    private void CheckProcesses()
    {
        // Получаем все процессы в Linux
        var runningProcesses = Process.GetProcesses().Select(p => p.ProcessName.ToLower()).ToHashSet();

        // Ищем, есть ли запущенная игра из нашей базы
        var activeProfile = _profiles.FirstOrDefault(p => runningProcesses.Contains(p.ProcessName));

        if (activeProfile != null && _currentActiveGame != activeProfile.ProcessName)
        {
            // Нашли новую игру! Применяем профиль
            Console.WriteLine($"[AutoProfile] Detected game: {activeProfile.DisplayName}. Applying profile...");
            _gpu.SetPowerLimit(activeProfile.TargetPowerLimit);
            _currentActiveGame = activeProfile.ProcessName;
        }
        else if (activeProfile == null && !string.IsNullOrEmpty(_currentActiveGame))
        {
            // Игра закрылась, возвращаем карту в тихий режим
            Console.WriteLine($"[AutoProfile] Game closed. Returning to desktop mode ({_desktopPowerLimit}W).");
            _gpu.SetPowerLimit(_desktopPowerLimit);
            _currentActiveGame = string.Empty;
        }
    }
}