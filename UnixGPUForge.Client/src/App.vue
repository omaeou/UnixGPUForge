<script setup>
import "./app-style.css";
import { ref, onMounted, onUnmounted } from "vue";
import { Menu, Home, Settings, Info, Cpu, Zap, Activity, MemoryStick, Trash2, Plus } from "lucide-vue-next";

// Импортируем наш компонент кривой
import FanCurveEditor from "./components/FanCurveEditor.vue";

const gpuData = ref(null);
const targetPowerLimit = ref(250);
const targetCoreClock = ref(2500);
const targetMemClock = ref(10000);
const isSidebarOpen = ref(true);
let intervalId = null;
const profiles = ref([]);
const newProfile = ref({ processName: '', powerLimit: 200, coreClock: 2500, memoryClock: 11000 });

// === ВЗАИМОДЕЙСТВИЕ С API ===

const fetchProfiles = async () => {
  try {
    const res = await fetch("http://localhost:5000/api/profiles");
    profiles.value = await res.json();
  } catch (err) {
    console.error("Ошибка загрузки профилей:", err);
  }
};

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

const addProfile = () => {
  if (!newProfile.value.processName) return;
  profiles.value.push({ ...newProfile.value });
  saveProfiles();
  newProfile.value.processName = ''; 
};

const deleteProfile = (index) => {
  profiles.value.splice(index, 1);
  saveProfiles();
};

const fetchMetrics = async () => {
  try {
    const response = await fetch("http://localhost:5000/api/gpu/metrics");
    const data = await response.json();

    if (gpuData.value === null) {
      targetPowerLimit.value = data.maxLimit;
    }
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
    if (!result.success) alert(result.message); 
  } catch (error) {
    console.error("Ошибка установки лимита:", error);
  }
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

const applyMemClockLock = async () => {
  try {
    const response = await fetch("http://localhost:5000/api/gpu/memclocklock", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ maxClock: parseInt(targetMemClock.value) }),
    });
    const result = await response.json();
    if (!result.success) alert(result.message);
  } catch (error) {
    console.error("Ошибка лока памяти:", error);
  }
};

const applyAllChanges = async () => {
  await applyPowerLimit();
  await applyClockLock();
  await applyMemClockLock();
};

const resetAllChanges = async () => {
  try {
    await fetch("http://localhost:5000/api/gpu/clockreset", { method: "POST" });
    await fetch("http://localhost:5000/api/gpu/memclockreset", { method: "POST" });
    alert("Частоты сброшены на заводские настройки!");
  } catch (error) {
    console.error("Ошибка сброса частот:", error);
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
  <div class="app-wrapper">
    <!-- Сайдбар -->
    <aside :class="['glass-sidebar', { collapsed: !isSidebarOpen }]">
      <div class="sidebar-header">
        <button class="icon-btn" @click="isSidebarOpen = !isSidebarOpen">
          <Menu size="24" />
        </button>
        <span class="brand-text" v-if="isSidebarOpen">UnixGPUForge</span>
      </div>
      
      <nav class="sidebar-menu">
        <a href="#" class="menu-item active">
          <Home size="20" />
          <span v-if="isSidebarOpen">Dashboard</span>
        </a>
        <a href="#" class="menu-item">
          <Settings size="20" />
          <span v-if="isSidebarOpen">Settings</span>
        </a>
        <a href="#" class="menu-item">
          <Info size="20" />
          <span v-if="isSidebarOpen">About</span>
        </a>
      </nav>
    </aside>

    <!-- Основной контент -->
    <main class="main-content">
      <div class="content-container" v-if="gpuData">
        
        <header class="page-header">
          <div>
            <h1 class="page-title">{{ gpuData.name }}</h1>
            <p class="page-subtitle">Hardware Telemetry & Control</p>
          </div>
        </header>

        <!-- Виджеты телеметрии -->
        <section class="telemetry-grid">
          <div class="glass-widget">
            <div class="widget-icon temp"><Activity size="24" /></div>
            <div class="widget-data">
              <span class="widget-value">{{ gpuData.temperature }}<small>°C</small></span>
              <span class="widget-label">Temperature</span>
            </div>
          </div>
          <div class="glass-widget">
            <div class="widget-icon power"><Zap size="24" /></div>
            <div class="widget-data">
              <span class="widget-value">{{ gpuData.powerUsage }}<small>W</small></span>
              <span class="widget-label">Power Draw</span>
            </div>
          </div>
          <div class="glass-widget">
            <div class="widget-icon core"><Cpu size="24" /></div>
            <div class="widget-data">
              <span class="widget-value">{{ gpuData.coreLoad }}<small>%</small></span>
              <span class="widget-label">Core Load</span>
            </div>
          </div>
          <div class="glass-widget">
            <div class="widget-icon mem"><MemoryStick size="24" /></div>
            <div class="widget-data">
              <span class="widget-value">{{ gpuData.vramUsed }}<small>MB</small></span>
              <span class="widget-label">VRAM Usage</span>
            </div>
          </div>
        </section>

        <div class="two-column-layout">
          <!-- Ручной тюнинг -->
          <section class="glass-card tuning-section">
            <h2 class="card-title">Manual Tuning</h2>
            
            <div class="slider-group">
              <div class="slider-header">
                <label for="power-limit-slider">Power Limit</label>
                <span class="slider-val">{{ targetPowerLimit }} W</span>
              </div>
              <input id="power-limit-slider" type="range" v-model="targetPowerLimit" :min="gpuData.minLimit" :max="gpuData.maxLimit" class="premium-slider" />
              <div class="slider-bounds"><span>{{ gpuData.minLimit }} W</span><span>{{ gpuData.maxLimit }} W</span></div>
            </div>

            <div class="slider-group">
              <div class="slider-header">
                <label for="core-clock-slider">Core Clock Limit</label>
                <span class="slider-val">{{ targetCoreClock }} MHz</span>
              </div>
              <input id="core-clock-slider" type="range" v-model="targetCoreClock" min="1000" max="3000" step="15" class="premium-slider" />
              <div class="slider-bounds"><span>1000 MHz</span><span>3000 MHz</span></div>
            </div>

            <div class="slider-group">
              <div class="slider-header">
                <label for="mem-clock-slider">Memory Clock</label>
                <span class="slider-val">{{ targetMemClock }} MHz</span>
              </div>
              <input id="mem-clock-slider" type="range" v-model="targetMemClock" min="5000" max="12500" step="50" class="premium-slider" />
              <div class="slider-bounds"><span>5000 MHz</span><span>12500 MHz</span></div>
            </div>

            <div class="card-actions">
              <button @click="resetAllChanges" class="btn btn-glass">Reset to Default</button>
              <button @click="applyAllChanges" class="btn btn-accent">Apply Settings</button>
            </div>
          </section>

          <!-- Fan Curve (Компонент) -->
          <section class="fan-curve-section">
            <FanCurveEditor />
          </section>
        </div>

        <!-- Автоматические профили -->
        <section class="glass-card profiles-section">
          <h2 class="card-title">Auto-Profiles</h2>
          <p class="card-subtitle" style="margin-bottom: 20px;">Динамическое переключение профилей по имени процесса</p>
          
          <div class="profiles-list">
            <div class="profile-row" v-for="(profile, index) in profiles" :key="index">
              <div class="profile-name">
                <div class="process-badge">{{ profile.processName.charAt(0).toUpperCase() }}</div>
                <strong>{{ profile.processName }}</strong>
              </div>
              <div class="profile-stats">
                <span><Zap size="14"/> {{ profile.powerLimit }}W</span>
                <span><Cpu size="14"/> {{ profile.coreClock }}MHz</span>
                <span><MemoryStick size="14"/> {{ profile.memoryClock }}MHz</span>
              </div>
              <button @click="deleteProfile(index)" class="icon-btn danger" title="Delete profile">
                <Trash2 size="18" />
              </button>
            </div>
          </div>

          <!-- Форма добавления -->
          <div class="add-profile-form">
            <div class="input-wrap process-wrap">
              <label for="new-proc">Process Name</label>
              <input id="new-proc" v-model="newProfile.processName" placeholder="e.g. dota2" />
            </div>
            <div class="input-wrap">
              <label for="new-pl">PL (W)</label>
              <input id="new-pl" v-model="newProfile.powerLimit" type="number" />
            </div>
            <div class="input-wrap">
              <label for="new-core">Core (MHz)</label>
              <input id="new-core" v-model="newProfile.coreClock" type="number" />
            </div>
            <div class="input-wrap">
              <label for="new-mem">Mem (MHz)</label>
              <input id="new-mem" v-model="newProfile.memoryClock" type="number" />
            </div>
            <button @click="addProfile" class="btn btn-accent icon-only">
              <Plus size="20" />
            </button>
          </div>
        </section>

      </div>
      
      <!-- Лоадер, если нет связи с демоном -->
      <div v-else class="loading-state">
        <div class="spinner"></div>
        <p>Connecting to UnixGPUForge Daemon...</p>
      </div>
    </main>
  </div>
</template>