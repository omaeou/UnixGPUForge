namespace UnixGPUForge.Shared.Models;

public class GameProfile
{
    public string ProcessName { get; set; } = string.Empty;
    public uint PowerLimit { get; set; }
    public uint CoreClock { get; set; }
    public uint MemoryClock { get; set; }
}