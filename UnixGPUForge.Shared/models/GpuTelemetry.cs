namespace UnixGPUForge.Shared.Models;

public record GpuTelemetry
{
    public string Name { get; init; } = string.Empty;
    public uint Temperature { get; init; }
    public uint PowerUsage { get; init; }
    public uint CoreLoad { get; init; }
    public ulong VramUsed { get; init; }
    public ulong VramTotal { get; init; }
    public uint MinLimit { get; init; }
    public uint MaxLimit { get; init; }
}