using System.Runtime.InteropServices;
using UnixGPUForge.Daemon.Core.Interfaces;
using UnixGPUForge.Shared.Models;

namespace UnixGPUForge.Daemon.Core.Providers;

public partial class NvidiaGpuProvider : IGpuProvider, IDisposable
{
    private const string NvmlLibrary = "libnvidia-ml.so";
    private readonly IntPtr _deviceHandle;

    [StructLayout(LayoutKind.Sequential)]
    private struct nvmlUtilization_t
    {
        public uint gpu;
        public uint memory;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct nvmlMemory_t
    {
        public ulong total;
        public ulong free;
        public ulong used;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct nvmlProcessInfo_t
    {
        public uint pid;
        public ulong usedGpuMemory;
        public uint gpuInstanceId;
        public uint computeInstanceId;
    }

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlInit_v2")]
    private static partial int NvmlInit();

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlShutdown")]
    private static partial int NvmlShutdown();

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
    private static partial int NvmlDeviceGetHandleByIndex(uint index, out IntPtr device);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetName", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int NvmlDeviceGetName(IntPtr device, IntPtr name, uint length);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetTemperature")]
    private static partial int NvmlDeviceGetTemperature(IntPtr device, uint sensorType, out uint temp);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetPowerUsage")]
    private static partial int NvmlDeviceGetPowerUsage(IntPtr device, out uint power);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetUtilizationRates")]
    private static partial int NvmlDeviceGetUtilizationRates(IntPtr device, out nvmlUtilization_t utilization);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetMemoryInfo")]
    private static partial int NvmlDeviceGetMemoryInfo(IntPtr device, out nvmlMemory_t memory);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetPowerManagementLimitConstraints")]
    private static partial int NvmlDeviceGetPowerManagementLimitConstraints(IntPtr device, out uint minLimit, out uint maxLimit);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceSetPowerManagementLimit_v2")]
    private static partial int NvmlDeviceSetPowerManagementLimit(IntPtr device, uint limit);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceSetGpuLockedClocks")]
    private static partial int NvmlDeviceSetGpuLockedClocks(IntPtr device, uint minGpuClockMHz, uint maxGpuClockMHz);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceResetGpuLockedClocks")]
    private static partial int NvmlDeviceResetGpuLockedClocks(IntPtr device);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceSetMemoryLockedClocks")]
    private static partial int NvmlDeviceSetMemoryLockedClocks(IntPtr device, uint minMemClockMHz, uint maxMemClockMHz);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceResetMemoryLockedClocks")]
    private static partial int NvmlDeviceResetMemoryLockedClocks(IntPtr device);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetGraphicsRunningProcesses_v3")]
    private static partial int NvmlDeviceGetGraphicsRunningProcesses(IntPtr device, ref uint infoCount, [Out] nvmlProcessInfo_t[] infos);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetComputeRunningProcesses_v3")]
    private static partial int NvmlDeviceGetComputeRunningProcesses(IntPtr device, ref uint infoCount, [Out] nvmlProcessInfo_t[] infos);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceSetFanSpeed_v2")]
    private static partial int NvmlDeviceSetFanSpeed(IntPtr device, uint fan, uint speed);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceSetDefaultFanSpeed_v2")]
    private static partial int NvmlDeviceSetDefaultFanSpeed(IntPtr device, uint fan);

    [LibraryImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetNumFans")]
    private static partial int NvmlDeviceGetNumFans(IntPtr device, out uint numFans);

    public NvidiaGpuProvider()
    {
        if (NvmlInit() != 0)
            throw new Exception("Не удалось инициализировать NVML драйвер.");

        if (NvmlDeviceGetHandleByIndex(0, out _deviceHandle) != 0)
            throw new Exception("Видеокарта NVIDIA не найдена.");
    }

    public GpuTelemetry GetTelemetry()
    {
        IntPtr namePtr = Marshal.AllocHGlobal(96);
        string gpuName = "Unknown GPU";
        if (NvmlDeviceGetName(_deviceHandle, namePtr, 96) == 0)
        {
            gpuName = Marshal.PtrToStringUTF8(namePtr) ?? gpuName;
        }
        Marshal.FreeHGlobal(namePtr);

        NvmlDeviceGetTemperature(_deviceHandle, 0, out uint temp);
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

    public bool SetFanSpeed(uint fanIndex, uint speedPercent)
    {
        int result = NvmlDeviceSetFanSpeed(_deviceHandle, fanIndex, speedPercent);
        if (result != 0)
        {
            Console.WriteLine($"[NVML] Ошибка управления винтом {fanIndex}. Код ошибки NVML: {result}");
            return false;
        }
        return true;
    }
    public bool ResetFanSpeed(uint fanIndex)
    {
        return NvmlDeviceSetDefaultFanSpeed(_deviceHandle, fanIndex) == 0;
    }

    public uint GetFanCount()
    {
        if (NvmlDeviceGetNumFans(_deviceHandle, out uint fanCount) == 0)
        {
            return fanCount;
        }
        
        return 2;
    }

    public List<uint> GetRunningPids()
    {
        var pids = new HashSet<uint>();
        var dummy = Array.Empty<nvmlProcessInfo_t>();

        uint graphicsCount = 0;
        int resGraphics = NvmlDeviceGetGraphicsRunningProcesses(_deviceHandle, ref graphicsCount, dummy);
        if (resGraphics == 7 && graphicsCount > 0)
        {
            var graphicsInfos = new nvmlProcessInfo_t[graphicsCount];
            if (NvmlDeviceGetGraphicsRunningProcesses(_deviceHandle, ref graphicsCount, graphicsInfos) == 0)
            {
                foreach (var info in graphicsInfos) pids.Add(info.pid);
            }
        }

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

    public void Dispose()
    {
        NvmlShutdown();
        GC.SuppressFinalize(this);
    }
}