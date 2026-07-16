namespace UnixGPUForge.Daemon.Core.Interfaces;

public interface IGpuProvider
{
    string GetDeviceName();
    uint GetCoreTemperature();
    uint GetPowerUsage();
    bool SetPowerLimit(uint watts);
    // Позже сюда добавим методы для управления кулерами и лимитами
    (uint Gpu, uint Memory) GetUtilization();
    (ulong Used, ulong Total) GetMemoryInfo();
    (uint Min, uint Max) GetPowerLimitConstraints();
    // Блокировка частоты ядра (от минимальной до максимальной)
    bool SetGpuLockedClocks(uint minMHz, uint maxMHz);
    // Сброс блокировки (возврат в автоматический режим)
    bool ResetGpuLockedClocks();
}