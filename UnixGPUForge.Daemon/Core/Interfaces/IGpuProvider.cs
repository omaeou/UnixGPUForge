using UnixGPUForge.Shared.Models;
namespace UnixGPUForge.Daemon.Core.Interfaces;

public interface IGpuProvider
{
    GpuTelemetry GetTelemetry();
    List<uint> GetRunningPids();
    bool SetPowerLimit(uint watts);
    bool SetGpuLockedClocks(uint minMHz, uint maxMHz);
    bool ResetGpuLockedClocks();
    bool SetMemoryLockedClocks(uint minMHz, uint maxMHz);
    bool ResetMemoryLockedClocks();
}