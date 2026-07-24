<script setup>
import { ref, onMounted, onUnmounted } from "vue";
import "./app-style.css";
import { Menu, Home, Settings, Info } from "@lucide/vue";

const gpuData = ref(null);
const targetPowerLimit = ref(250);
const targetCoreClock = ref(2500);
const isSidebarOpen = ref(false);
let intervalId = null;
const profiles = ref([]);
const newProfile = ref({ processName: '', powerLimit: 200, coreClock: 2500, memoryClock: 11000 });


// Загрузка профилей с бэкенда
const fetchProfiles = async () => {
  try {
    const res = await fetch("http://localhost:5000/api/profiles");
    profiles.value = await res.json();
  } catch (err) {
    console.error("Ошибка загрузки профилей:", err);
  }
};

// Сохранение профилей на бэкенд
const saveProfiles = async () => {
  try {
    await fetch("http://localhost:5000/api/profiles", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(profiles.value),
    });
  } catch (err) {
    console.error("Ошибка сохранения профилей:", err);
  }
};

// Добавление нового
const addProfile = () => {
  if (!newProfile.value.processName) return;
  profiles.value.push({ ...newProfile.value });
  saveProfiles();
  newProfile.value.processName = ''; // Очищаем только имя процесса для удобства
};

// Удаление
const deleteProfile = (index) => {
  profiles.value.splice(index, 1);
  saveProfiles();
};

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
  fetchProfiles();
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
      
      <!-- Карточка телеметрии -->
      <div v-if="gpuData" class="GpuInfo-card">
        <h2>GPU: {{ gpuData.name }}</h2>
        <div class="GpuMetrics-column">
          <p>Temp: {{ gpuData.temperature }}°C</p>
          <p>Power Usage: {{ gpuData.powerUsage }} W</p>
          <p>Core Load: {{ gpuData.coreLoad }}%</p>
          <p>VRAM: {{ gpuData.vramUsed }} / {{ gpuData.vramTotal }} MB</p>
        </div>
      </div>

      <!-- Карточка ручного тюнинга -->
      <div v-if="gpuData" class="control-panel-card" style="margin-top: 2rem;">
        <div class="params-element">
          <div class="param-header">
            <h3 class="param-title">Ручной тюнинг</h3>
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
                :min="gpuData.minLimit"
                :max="gpuData.maxLimit"
                class="slider"
              />
            </div>
            <div class="limit-info">Лимиты: {{ gpuData.minLimit }}W — {{ gpuData.maxLimit }}W</div>
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

          <div class="setting-container">
            <div class="slider-row">
              <label for="mem-clock-slider" class="slider-label">
                Memory Clock: <span class="highlight">{{ targetMemClock }} MHz</span>
              </label>
              <input
                id="mem-clock-slider"
                type="range"
                v-model="targetMemClock"
                min="5000"
                max="12500"
                step="50" 
                class="slider"
              />
            </div>
            <div class="limit-info">Абсолютная частота видеопамяти (GDDR6X)</div>
          </div>

          <div class="action-row" style="gap: 15px; margin-top: 1.5rem;">
            <button @click="resetAllChanges" class="btn secondary-btn" style="background: transparent; border: 1px solid var(--border-color); color: var(--primary-text);">
              Reset Clocks
            </button>
            <button @click="applyAllChanges" class="btn">
              Apply All Changes
            </button>
          </div>
        </div>
      </div>

      <!-- Карточка автоматических профилей -->
      <div v-if="gpuData" class="GpuInfo-card" style="margin-top: 2rem;">
        <h2>Автоматические профили игр</h2>
        
        <div class="setting-container" v-for="(profile, index) in profiles" :key="index" style="margin-bottom: 0.5rem; padding: 1rem; border: 1px solid var(--border-color); border-radius: 8px;">
          <div class="slider-row" style="justify-content: flex-start; gap: 2rem; align-items: center;">
            <strong style="width: 150px; color: var(--primary);">{{ profile.processName }}</strong>
            <span>PL: {{ profile.powerLimit }}W</span>
            <span>Core: {{ profile.coreClock }}MHz</span>
            <span>Mem: {{ profile.memoryClock }}MHz</span>
            
            <button @click="deleteProfile(index)" class="btn secondary-btn" style="margin-left: auto; padding: 0.4rem 1rem; font-size: 0.9rem; background: transparent; border: 1px solid #f38ba8; color: #f38ba8;">
              Удалить
            </button>
          </div>
        </div>

        <!-- Форма добавления нового профиля -->
        <div class="setting-container" style="display: flex; gap: 1rem; align-items: center; margin-top: 1.5rem; padding-top: 1.5rem; border-top: 1px solid var(--border-color);">
          <input v-model="newProfile.processName" placeholder="Процесс (напр. dota2)" class="input-field" style="flex: 2;" />
          <input v-model="newProfile.powerLimit" type="number" placeholder="PL (W)" class="input-field" style="flex: 1;" />
          <input v-model="newProfile.coreClock" type="number" placeholder="Core (MHz)" class="input-field" style="flex: 1;" />
          <input v-model="newProfile.memoryClock" type="number" placeholder="Mem (MHz)" class="input-field" style="flex: 1;" />
          <button @click="addProfile" class="btn" style="flex: 1;">Добавить</button>
        </div>
      </div>

    </div>
  </div>
</template>