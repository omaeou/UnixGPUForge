# UnixGPUForge
*[Read in English](#english) | [Читать на русском](#russian)*

<a id="english">
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
</a>
<a id="russian">
   
# UnixGPUForge

> **Продвинутый демон управления питанием, разгоном и телеметрией GPU для Linux.**

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Linux-lightgrey.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)
![Vue.js](https://img.shields.io/badge/Vue.js-UI-4FC08D.svg)

UnixGPUForge — это системная утилита, созданная для решения проблемы отсутствия надежных и удобных инструментов тонкой настройки видеокарт (undervolting, кастомные кривые вентиляторов) в среде Linux. 

В отличие от существующих решений, проект не является монолитным GUI-приложением. Он работает как независимый фоновый демон, обеспечивая применение настроек аппаратуры независимо от того, запущен ли графический сервер (X11/Wayland) и выполнен ли вход пользователя в систему.

## 🏗 Архитектура

Проект построен на базе клиент-серверной архитектуры для обеспечения безопасности и отказоустойчивости при работе с системными интерфейсами ядра.

* **UnixGPUForge.Daemon (.NET 10.0):** Системный сервис (daemon), обладающий необходимыми правами для работы с драйверами (включая модуль `NvidiaGpuProvider`). Отвечает за сбор телеметрии, автоматическое переключение профилей (`AutoProfileService`) и применение низкоуровневых параметров.
* **UnixGPUForge.Client (Vue.js / Vite):** Легковесный реактивный веб-клиент для визуализации состояния GPU в реальном времени и управления настройками демона через API.

## ✨ Основные возможности

- **Аппаратная телеметрия:** Точный мониторинг частот, температур и энергопотребления.
- **Авто-профилирование:** Механизм `GameProfile` автоматически переключает состояния видеокарты (например, профиль тишины для работы и профиль максимальной производительности при запуске тяжелых приложений).
- **Независимость от Desktop Environment:** Демон поддерживает заданные параметры охлаждения на уровне системы, что исключает перегрев при падении оконного менеджера.
- **Масштабируемость:** Интерфейс `IGpuProvider` закладывает фундамент для поддержки других архитектур GPU в будущих релизах.

## 🛠 Сборка проекта

**Требования:**
- ОС Linux (Ядро 5.15+)
- .NET 10.0 SDK
- Node.js & npm (для сборки клиента)
- Проприетарные драйверы NVIDIA (для полного функционала)


1. **Build the Daemon:**
   ```bash
   cd UnixGPUForge.Daemon
   dotnet build -c Release
</a>
