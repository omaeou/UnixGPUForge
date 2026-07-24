# UnixGPUForge
*[Read in English](#english) | [Читать на русском](#русский-ru)*

<a id="english"></a>
> **Advanced GPU Power Management, Overclocking, and Telemetry Daemon for Linux.**

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Linux-lightgrey.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)
![Vue.js](https://img.shields.io/badge/Vue.js-UI-4FC08D.svg)

UnixGPUForge is a robust, daemonized system utility designed to bridge the gap in advanced GPU power management and telemetry on Linux environments. Unlike traditional CLI-only tools or monolithic GUI applications, UnixGPUForge operates as a highly privileged background service with a decoupled, lightweight web-based client, ensuring minimal overhead and maximum system stability.

## 🏗 System Architecture

The project is strictly divided into a Client-Server architecture to maintain system security and stability while interacting with low-level kernel interfaces.

1. **UnixGPUForge.Daemon (C# / .NET 10.0)**
   - Runs as a `systemd` background service with necessary privileges to access `/sys/class/drm`, NVML, and proprietary driver interfaces.
   - Handles continuous hardware polling, telemetry aggregation (`GpuTelemetry.cs`), and state enforcement without user session dependency.
   - Exposes a secure local REST/gRPC API for client interactions.

2. **UnixGPUForge.Client (Vue.js / Vite)**
   - A reactive, zero-overhead frontend that communicates with the local daemon.
   - Provides real-time visualization of GPU metrics (Temperatures, Core/Memory Clocks, Power Draw, Fan Speeds).

## ✨ Key Features

- **Hardware-Level Access:** Direct interfacing with NVIDIA GPUs (`NvidiaGpuProvider`) for precise telemetry and tuning.
- **Auto-Profiling Service:** Dynamic, rule-based profile switching (`AutoProfileService`) based on current system load or running applications (`GameProfile`).
- **Headless Operation:** The daemon maintains custom fan curves and undervolting states even when no X11/Wayland session is active.
- **Cross-Driver Abstraction:** Designed with extensibility in mind via the `IGpuProvider` interface to seamlessly support different vendor architectures in the future.

## 🚀 Getting Started

### Prerequisites
- Linux OS (Kernel 5.15+)
- .NET 10.0 SDK
- Node.js & npm (for building the client)
- Proprietary NVIDIA drivers (for full feature support)

### Building from Source

1. **Build the Daemon:**
   ```bash
   cd UnixGPUForge.Daemon
   dotnet build -c Release
