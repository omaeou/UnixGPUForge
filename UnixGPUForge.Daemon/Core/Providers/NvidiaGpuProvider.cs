using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnixGPUForge.Daemon.Core.Interfaces;

namespace UnixGPUForge.Daemon.Core.Providers;

public partial class NvidiaGpuProvider : IGpuProvider
{
    private const string NvmlLibrary = "libnvidia-ml.so";
    private readonly IntPtr _deviceHandle;

    public NvidiaGpuProvider()
    {
        if (NvmlInit() != 0) throw new Exception("Failed to initialize NVML.");
        if (NvmlDeviceGetCount(out uint deviceCount) != 0 || deviceCount == 0) throw new Exception("No NVIDIA GPUs found.");
        if (NvmlDeviceGetHandleByIndex(0, out _deviceHandle) != 0) throw new Exception("Failed to get GPU handle.");
    }

    public string GetDeviceName()
    {
        unsafe
        {
            byte* nameBytes = stackalloc byte[96];
            if (NvmlDeviceGetName(_deviceHandle, nameBytes, 96) == 0)
                return Encoding.ASCII.GetString(nameBytes, 96).TrimEnd('\0');
        }
        return "Unknown GPU";
    }

    public uint GetCoreTemperature()
    {
        if (NvmlDeviceGetTemperature(_deviceHandle, 0, out uint temp) == 0) return temp;
        return 0;
    }

    public uint GetPowerUsage()
    {
        // NVML возвращает потребление в милливаттах (mW), делим на 1000 для обычных Ватт
        if (NvmlDeviceGetPowerUsage(_deviceHandle, out uint power) == 0) return power / 1000;
        return 0;
    }

    public (uint Gpu, uint Memory) GetUtilization()
    {
        if (NvmlDeviceGetUtilizationRates(_deviceHandle, out NvmlUtilization util) == 0)
            return (util.Gpu, util.Memory);
        return (0, 0);
    }

    public (ulong Used, ulong Total) GetMemoryInfo()
    {
        if (NvmlDeviceGetMemoryInfo(_deviceHandle, out NvmlMemory mem) == 0)
            // Возвращаем в Мегабайтах (делим байты на 1024*1024)
            return (mem.Used / 1048576, mem.Total / 1048576); 
        return (0, 0);
    }

    public bool SetPowerLimit(uint watts)
    {
        // Переводим Ватты обратно в милливатты
        int result = NvmlDeviceSetPowerManagementLimit(_deviceHandle, watts * 1000);
        if (result != 0)
        {
            Console.WriteLine($"[NVML ERROR] Failed to set power limit to {watts}W. NVML Error Code: {result}");
        }
        return result == 0;
    }

    public (uint Min, uint Max) GetPowerLimitConstraints()
    {
        if (NvmlDeviceGetPowerManagementLimitConstraints(_deviceHandle, out uint min, out uint max) == 0)
        {
            return (min / 1000, max / 1000); // Переводим из mW в Ватты
        }
        return (100, 350); // Fallback, если что-то пошло не так
    }

    public bool SetGpuLockedClocks(uint minMHz, uint maxMHz)
    {
        return NvmlDeviceSetGpuLockedClocks(_deviceHandle, minMHz, maxMHz) == 0;
    }

    public bool ResetGpuLockedClocks()
    {
        return NvmlDeviceResetGpuLockedClocks(_deviceHandle) == 0;
    }

    // ==========================================
    // C-Структуры для NVML
    // ==========================================
    [StructLayout(LayoutKind.Sequential)]
    private struct NvmlUtilization
    {
        public uint Gpu;
        public uint Memory;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NvmlMemory
    {
        public ulong Total;
        public ulong Free;
        public ulong Used;
    }

    // ==========================================
    // Нативные вызовы (LibraryImport)
    // ==========================================
    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlInit_v2")]
    private static partial int NvmlInit();

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetCount_v2")]
    private static partial int NvmlDeviceGetCount(out uint deviceCount);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
    private static partial int NvmlDeviceGetHandleByIndex(uint index, out IntPtr device);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetName")]
    private static unsafe partial int NvmlDeviceGetName(IntPtr device, byte* name, uint length);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetTemperature")]
    private static partial int NvmlDeviceGetTemperature(IntPtr device, int sensorType, out uint temp);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetPowerUsage")]
    private static partial int NvmlDeviceGetPowerUsage(IntPtr device, out uint power);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetUtilizationRates")]
    private static partial int NvmlDeviceGetUtilizationRates(IntPtr device, out NvmlUtilization utilization);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetMemoryInfo")]
    private static partial int NvmlDeviceGetMemoryInfo(IntPtr device, out NvmlMemory memory);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceSetPowerManagementLimit")]
    private static partial int NvmlDeviceSetPowerManagementLimit(IntPtr device, uint limitMw);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetPowerManagementLimitConstraints")]
    private static partial int NvmlDeviceGetPowerManagementLimitConstraints(IntPtr device, out uint minLimit, out uint maxLimit);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceSetGpuLockedClocks")]
    private static partial int NvmlDeviceSetGpuLockedClocks(IntPtr device, uint minGpuClockMHz, uint maxGpuClockMHz);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceResetGpuLockedClocks")]
    private static partial int NvmlDeviceResetGpuLockedClocks(IntPtr device);
}