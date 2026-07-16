<script setup>
import { ref, onMounted, onUnmounted } from "vue";
import "./app-style.css";
import { Menu, Home, Settings, Info } from "@lucide/vue";

const gpuData = ref(null);
const targetPowerLimit = ref(250);
const targetCoreClock = ref(2500);
const isSidebarOpen = ref(false);
let intervalId = null;

const applyClockLock = async () => {
  try {
    const response = await fetch("http://localhost:5000/api/gpu/clocklock", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ maxClock: parseInt(targetCoreClock.value) }),
    });
    const result = await response.json();
    if (!result.success) alert(result.message);
  } catch (error) {
    console.error("Ошибка лока частот:", error);
  }
};

const resetClockLock = async () => {
  try {
    const response = await fetch("http://localhost:5000/api/gpu/clockreset", { method: "POST" });
    const result = await response.json();
    if (result.success) {
      alert("Частоты сброшены на заводские настройки!");
    }
  } catch (error) {
    console.error("Ошибка сброса частот:", error);
  }
};

const fetchMetrics = async () => {
  try {
    const response = await fetch("http://localhost:5000/api/gpu/metrics");
    const data = await response.json();

    // При первой загрузке ставим ползунок на максимальный доступный лимит
    if (gpuData.value === null) {
      targetPowerLimit.value = data.maxLimit;
    }

    // Обновляем метрики
    gpuData.value = data;
  } catch (error) {
    console.error("Ошибка подключения к демону:", error);
  }
};

const applyPowerLimit = async () => {
  try {
    const response = await fetch("http://localhost:5000/api/gpu/powerlimit", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ watts: parseInt(targetPowerLimit.value) }),
    });

    const result = await response.json();
    if (!result.success) {
      alert(result.message); // Выдаст ошибку, если нет прав root
    }
  } catch (error) {
    console.error("Ошибка установки лимита:", error);
  }
};

onMounted(() => {
  fetchMetrics();
  intervalId = setInterval(fetchMetrics, 1000);
});

onUnmounted(() => {
  clearInterval(intervalId);
});
</script>

<template>
  <div class="home-container">
    <!-- Сайдбар -->
    <div :class="['sidebar', { collapsed: !isSidebarOpen }]">
      <button class="sidebar-toggle" @click="isSidebarOpen = !isSidebarOpen">
        <Menu size="28" color="var(--primary-text)" />
      </button>
      
      <ul class="sidebar-menu">
        <li class="sidebar-item active">
          <Home size="22" class="sidebar-icon" />
          <span class="sidebar-text">Главная</span>
        </li>
        <li class="sidebar-item">
          <Settings size="22" class="sidebar-icon" />
          <span class="sidebar-text">Настройки</span>
        </li>
        <li class="sidebar-item">
          <Info size="22" class="sidebar-icon" />
          <span class="sidebar-text">О программе</span>
        </li>
      </ul>
    </div>

    <!-- Основной контент -->
    <div class="home-content">
      <div v-if="gpuData" class="GpuInfo-card">
        <h2>GPU: {{ gpuData.name }}</h2>
        <div class="GpuMetrics-column">
          <p>Temp: {{ gpuData.temperature }}°C</p>
          <p>Power Usage: {{ gpuData.powerUsage }} W</p>
          <p>Core Load: {{ gpuData.coreLoad }}%</p>
          <p>VRAM: {{ gpuData.vramUsed }} / {{ gpuData.vramTotal }} MB</p>
        </div>
      </div>

      <div v-if="gpuData" class="control-panel-card">
        <div class="params-element">
          <div class="param-header">
            <h3 class="param-title">Настройка лимита мощности</h3>
          </div>
          <div class="setting-container">
            <div class="slider-row">
              <label for="power-limit-slider" class="slider-label">
                Power Limit: <span class="highlight">{{ targetPowerLimit }} W</span>
              </label>
              <input
                id="power-limit-slider"
                type="range"
                v-model="targetPowerLimit"
                :min="gpuData.powerLimitMin"
                :max="gpuData.powerLimitMax"
                class="slider"
              />
            </div>
            <div class="limit-info">Лимиты: {{ gpuData.powerLimitMin }}W — {{ gpuData.powerLimitMax }}W</div>
          </div>
          <div class="setting-container">
            <div class="slider-row">
              <label for="core-clock-slider" class="slider-label">
                Core Clock Limit: <span class="highlight">{{ targetCoreClock }} MHz</span>
              </label>
              <input
                id="core-clock-slider"
                type="range"
                v-model="targetCoreClock"
                min="1000"
                max="3000"
                step="15" 
                class="slider"
              />
            </div>
            <div class="limit-info">Фиксация максимальной частоты ядра (шаг 15 MHz)</div>
          </div>
          <div class="action-row">
            <button @click="applyPowerLimit" class="btn">Apply Changes</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
