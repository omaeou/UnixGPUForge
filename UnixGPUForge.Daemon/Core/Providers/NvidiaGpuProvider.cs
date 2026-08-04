using System.Runtime.InteropServices;
using UnixGPUForge.Daemon.Core.Interfaces;
using UnixGPUForge.Shared.Models;

namespace UnixGPUForge.Daemon.Core.Providers;

public partial class NvidiaGpuProvider : IGpuProvider
{
    private const string NvmlLibrary = "libnvidia-ml.so";
    private readonly IntPtr _deviceHandle;

    // --- NVML СТРУКТУРЫ ИЗ ДОКУМЕНТАЦИИ ---
    [StructLayout(LayoutKind.Sequential)]
    private struct nvmlUtilization_t //[cite: 1]
    {
        public uint gpu;
        public uint memory;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct nvmlMemory_t //[cite: 1]
    {
        public ulong total;
        public ulong free;
        public ulong used;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct nvmlProcessInfo_t //[cite: 1]
    {
        public uint pid;
        public ulong usedGpuMemory;
        public uint gpuInstanceId;
        public uint computeInstanceId;
    }

    // --- ИМПОРТЫ C-ФУНКЦИЙ NVML ---
    
    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlInit_v2")]
    private static partial int NvmlInit(); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlShutdown")]
    private static partial int NvmlShutdown(); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
    private static partial int NvmlDeviceGetHandleByIndex(uint index, out IntPtr device); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetName", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int NvmlDeviceGetName(IntPtr device, IntPtr name, uint length); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetTemperature")]
    private static partial int NvmlDeviceGetTemperature(IntPtr device, uint sensorType, out uint temp); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetPowerUsage")]
    private static partial int NvmlDeviceGetPowerUsage(IntPtr device, out uint power); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetUtilizationRates")]
    private static partial int NvmlDeviceGetUtilizationRates(IntPtr device, out nvmlUtilization_t utilization); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetMemoryInfo")]
    private static partial int NvmlDeviceGetMemoryInfo(IntPtr device, out nvmlMemory_t memory); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetPowerManagementLimitConstraints")]
    private static partial int NvmlDeviceGetPowerManagementLimitConstraints(IntPtr device, out uint minLimit, out uint maxLimit); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceSetPowerManagementLimit_v2")]
    private static partial int NvmlDeviceSetPowerManagementLimit(IntPtr device, uint limit); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceSetGpuLockedClocks")]
    private static partial int NvmlDeviceSetGpuLockedClocks(IntPtr device, uint minGpuClockMHz, uint maxGpuClockMHz); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceResetGpuLockedClocks")]
    private static partial int NvmlDeviceResetGpuLockedClocks(IntPtr device); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceSetMemoryLockedClocks")]
    private static partial int NvmlDeviceSetMemoryLockedClocks(IntPtr device, uint minMemClockMHz, uint maxMemClockMHz); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceResetMemoryLockedClocks")]
    private static partial int NvmlDeviceResetMemoryLockedClocks(IntPtr device); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetGraphicsRunningProcesses_v3")]
    private static partial int NvmlDeviceGetGraphicsRunningProcesses(IntPtr device, ref uint infoCount, [Out] nvmlProcessInfo_t[] infos); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetComputeRunningProcesses_v3")]
    private static partial int NvmlDeviceGetComputeRunningProcesses(IntPtr device, ref uint infoCount, [Out] nvmlProcessInfo_t[] infos); //[cite: 1]

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceSetFanSpeed_v2")]
    private static partial int NvmlDeviceSetFanSpeed(IntPtr device, uint fan, uint speed);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceSetDefaultFanSpeed_v2")]
    private static partial int NvmlDeviceSetDefaultFanSpeed(IntPtr device, uint fan);
    public NvidiaGpuProvider()
    {
        if (NvmlInit() != 0)
            throw new Exception("Не удалось инициализировать NVML драйвер.");

        // Подхватываем первую видеокарту (индекс 0)
        if (NvmlDeviceGetHandleByIndex(0, out _deviceHandle) != 0)
            throw new Exception("Видеокарта NVIDIA не найдена.");
    }

    public GpuTelemetry GetTelemetry()
    {
        IntPtr namePtr = Marshal.AllocHGlobal(96); // NVML_DEVICE_NAME_V2_BUFFER_SIZE = 96[cite: 1]
        string gpuName = "Unknown GPU";
        if (NvmlDeviceGetName(_deviceHandle, namePtr, 96) == 0)
        {
            gpuName = Marshal.PtrToStringUTF8(namePtr) ?? gpuName;
        }
        Marshal.FreeHGlobal(namePtr);

        NvmlDeviceGetTemperature(_deviceHandle, 0, out uint temp); // 0 = NVML_TEMPERATURE_GPU[cite: 1]
        NvmlDeviceGetPowerUsage(_deviceHandle, out uint powerMw);
        NvmlDeviceGetUtilizationRates(_deviceHandle, out nvmlUtilization_t util);
        NvmlDeviceGetMemoryInfo(_deviceHandle, out nvmlMemory_t mem);
        NvmlDeviceGetPowerManagementLimitConstraints(_deviceHandle, out uint minLimitMw, out uint maxLimitMw);

        return new GpuTelemetry
        {
            Name = gpuName,
            Temperature = temp,
            PowerUsage = powerMw / 1000,
            CoreLoad = util.gpu,
            VramUsed = mem.used / (1024 * 1024),
            VramTotal = mem.total / (1024 * 1024),
            MinLimit = minLimitMw / 1000,
            MaxLimit = maxLimitMw / 1000
        };
    }

    public bool SetPowerLimit(uint watts)
    {
        return NvmlDeviceSetPowerManagementLimit(_deviceHandle, watts * 1000) == 0;
    }

    public bool SetGpuLockedClocks(uint minMHz, uint maxMHz)
    {
        return NvmlDeviceSetGpuLockedClocks(_deviceHandle, minMHz, maxMHz) == 0;
    }

    public bool ResetGpuLockedClocks()
    {
        return NvmlDeviceResetGpuLockedClocks(_deviceHandle) == 0;
    }

    public bool SetMemoryLockedClocks(uint minMHz, uint maxMHz)
    {
        return NvmlDeviceSetMemoryLockedClocks(_deviceHandle, minMHz, maxMHz) == 0;
    }

    public bool ResetMemoryLockedClocks()
    {
        return NvmlDeviceResetMemoryLockedClocks(_deviceHandle) == 0;
    }
    public void Dispose()
    {
        NvmlShutdown(); // Корректно освобождаем ресурсы драйвера[cite: 1]
        GC.SuppressFinalize(this);
    }

    public bool SetFanSpeed(uint fanIndex, uint speedPercent)
    {
        // speedPercent от 0 до 100
        return NvmlDeviceSetFanSpeed(_deviceHandle, fanIndex, speedPercent) == 0;
    }

    public bool ResetFanSpeed(uint fanIndex)
    {
        // Возвращаем управление автоматике драйвера
        return NvmlDeviceSetDefaultFanSpeed(_deviceHandle, fanIndex) == 0;
    }
    public List<uint> GetRunningPids()
    {
        var pids = new HashSet<uint>();
        var dummy = Array.Empty<nvmlProcessInfo_t>();

        // 1. Собираем графические процессы
        uint graphicsCount = 0;
        int resGraphics = NvmlDeviceGetGraphicsRunningProcesses(_deviceHandle, ref graphicsCount, dummy);
        // Код 7 = NVML_ERROR_INSUFFICIENT_SIZE (буфер мал, но мы узнали нужное количество)[cite: 1]
        if (resGraphics == 7 && graphicsCount > 0)
        {
            var graphicsInfos = new nvmlProcessInfo_t[graphicsCount];
            if (NvmlDeviceGetGraphicsRunningProcesses(_deviceHandle, ref graphicsCount, graphicsInfos) == 0)
            {
                foreach (var info in graphicsInfos) pids.Add(info.pid);
            }
        }

        // 2. Собираем вычислительные (Compute) процессы
        uint computeCount = 0;
        int resCompute = NvmlDeviceGetComputeRunningProcesses(_deviceHandle, ref computeCount, dummy);
        if (resCompute == 7 && computeCount > 0)
        {
            var computeInfos = new nvmlProcessInfo_t[computeCount];
            if (NvmlDeviceGetComputeRunningProcesses(_deviceHandle, ref computeCount, computeInfos) == 0)
            {
                foreach (var info in computeInfos) pids.Add(info.pid);
            }
        }

        return pids.ToList();
    }
}