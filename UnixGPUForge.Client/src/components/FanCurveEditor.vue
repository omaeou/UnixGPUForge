<template>
  <div class="fan-curve-card">
    <div class="card-header">
      <h3 class="card-title">Fan Curve Control</h3>
      <span class="card-subtitle">Интеллектуальное управление охлаждением</span>
    </div>
    
    <div class="chart-outer">
      <!-- Внутренний контейнер графика -->
      <div 
        class="chart-inner" 
        ref="wrapperRef" 
        @mousemove="onMouseMove" 
        @mouseup="stopDrag" 
        @mouseleave="stopDrag"
      >
        
        <!-- Мягкая сетка на фоне (HTML) -->
        <div class="grid-lines">
          <div class="grid-line-h" style="bottom: 0%"></div>
          <div class="grid-line-h" style="bottom: 25%"></div>
          <div class="grid-line-h" style="bottom: 50%"></div>
          <div class="grid-line-h" style="bottom: 75%"></div>
          <div class="grid-line-h" style="bottom: 100%"></div>
          
          <div class="grid-line-v" style="left: 25%"></div>
          <div class="grid-line-v" style="left: 50%"></div>
          <div class="grid-line-v" style="left: 75%"></div>
        </div>

        <!-- SVG для отрисовки только линии и заливки -->
        <svg class="curve-svg" viewBox="0 0 100 100" preserveAspectRatio="none">
          <defs>
            <linearGradient id="curveGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stop-color="var(--accent)" stop-opacity="0.4" />
              <stop offset="100%" stop-color="var(--accent)" stop-opacity="0.0" />
            </linearGradient>
            <filter id="glow" x="-20%" y="-20%" width="140%" height="140%">
              <feGaussianBlur stdDeviation="1.5" result="blur" />
              <feComposite in="SourceGraphic" in2="blur" operator="over" />
            </filter>
          </defs>
          
          <!-- Заливка и линия -->
          <polygon :points="fillPolygonPoints" class="curve-fill" />
          <polyline :points="linePolygonPoints" class="curve-line" filter="url(#glow)" />
        </svg>

        <!-- HTML Точки (Никогда не сплющатся) -->
        <div 
          v-for="(point, index) in points" 
          :key="index"
          class="html-point-wrapper"
          :class="{ 'is-active': isInteracting(index) }"
          :style="{ left: point.temperature + '%', bottom: point.fanSpeedPercent + '%' }"
          @mousedown.stop="startDrag(index)"
          @mouseenter="hoveredIndex = index"
          @mouseleave="hoveredIndex = draggingIndex !== null ? draggingIndex : null"
        >
          <div class="html-point-core"></div>
        </div>

        <!-- Изящный Tooltip -->
        <div 
          v-if="activePoint" 
          class="html-tooltip"
          :style="{ left: tooltipX + '%', bottom: activePoint.fanSpeedPercent + '%' }"
        >
          {{ activePoint.temperature }}°C <span class="divider">|</span> {{ activePoint.fanSpeedPercent }}%
        </div>

        <!-- Подписи осей X и Y -->
        <div class="axis-x-labels">
          <span style="left: 0%">0°C</span>
          <span style="left: 25%">25°C</span>
          <span style="left: 50%">50°C</span>
          <span style="left: 75%">75°C</span>
          <span style="left: 100%">100°C</span>
        </div>
        <div class="axis-y-labels">
          <span style="bottom: 100%">100%</span>
          <span style="bottom: 75%">75%</span>
          <span style="bottom: 50%">50%</span>
          <span style="bottom: 25%">25%</span>
          <span style="bottom: 0%">0%</span>
        </div>

      </div>
    </div>

    <!-- Редактор точек -->
    <div class="controls-panel">
      <div class="points-list">
        <div v-for="(point, index) in points" :key="'list-'+index" class="point-item">
          <div class="point-badge">{{ index + 1 }}</div>
          <div class="input-group">
            <label :for="'temp-' + index">Temp °C</label>
            <input :id="'temp-' + index" type="number" v-model.number="point.temperature" min="0" max="100" @change="sortPoints" />
          </div>
          <div class="input-group">
            <label :for="'speed-' + index">Speed %</label>
            <input :id="'speed-' + index" type="number" v-model.number="point.fanSpeedPercent" min="0" max="100" />
          </div>
          <button @click="removePoint(index)" class="btn-icon btn-danger" :disabled="points.length <= 2" title="Remove">
            ✕
          </button>
        </div>
      </div>

      <div class="actions-row">
        <button @click="addPoint" class="btn btn-glass">+ Add Point</button>
        <button @click="saveCurve" class="btn btn-accent">Apply Fan Curve</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';

const points = ref([]);
const wrapperRef = ref(null);
const draggingIndex = ref(null);
const hoveredIndex = ref(null);

// Координаты для SVG (Y идет сверху вниз, поэтому 100 - percent)
const linePolygonPoints = computed(() => {
  return points.value.map(p => `${p.temperature},${100 - p.fanSpeedPercent}`).join(' ');
});

const fillPolygonPoints = computed(() => {
  if (points.value.length === 0) return '';
  const start = `${points.value[0].temperature},100`;
  const end = `${points.value[points.value.length - 1].temperature},100`;
  return `${start} ${linePolygonPoints.value} ${end}`;
});

const isInteracting = (index) => draggingIndex.value === index || hoveredIndex.value === index;

const activePoint = computed(() => {
  const index = draggingIndex.value !== null ? draggingIndex.value : hoveredIndex.value;
  return index !== null ? points.value[index] : null;
});

// Слегка корректируем X тултипа у краев, чтобы не обрезался
const tooltipX = computed(() => {
  if (!activePoint.value) return 0;
  let x = activePoint.value.temperature;
  if (x < 5) return 5;
  if (x > 95) return 95;
  return x;
});

// === ЛОГИКА DRAG & DROP (Чистые проценты) ===
const startDrag = (index) => {
  draggingIndex.value = index;
  hoveredIndex.value = index;
};

const stopDrag = () => {
  draggingIndex.value = null;
  sortPoints(); 
};

const onMouseMove = (event) => {
  if (draggingIndex.value === null || !wrapperRef.value) return;

  const rect = wrapperRef.value.getBoundingClientRect();
  
  // Расчет мыши строго внутри прямоугольника
  let xPercent = ((event.clientX - rect.left) / rect.width) * 100;
  let yPercent = 100 - (((event.clientY - rect.top) / rect.height) * 100);

  xPercent = Math.max(0, Math.min(100, xPercent));
  yPercent = Math.max(0, Math.min(100, yPercent));

  const minX = draggingIndex.value > 0 ? points.value[draggingIndex.value - 1].temperature : 0;
  const maxX = draggingIndex.value < points.value.length - 1 ? points.value[draggingIndex.value + 1].temperature : 100;

  points.value[draggingIndex.value].temperature = Math.round(Math.max(minX, Math.min(maxX, xPercent)));
  points.value[draggingIndex.value].fanSpeedPercent = Math.round(yPercent);
};

// === API ===
const sortPoints = () => points.value.sort((a, b) => a.temperature - b.temperature);

const fetchCurve = async () => {
  try {
    const res = await fetch('http://localhost:5000/api/fancurve');
    points.value = await res.json();
    sortPoints();
  } catch (error) {
    console.error("API Error:", error);
  }
};

const saveCurve = async () => {
  try {
    sortPoints();
    await fetch('http://localhost:5000/api/fancurve', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(points.value)
    });
  } catch (error) {
    console.error("API Error:", error);
  }
};

const addPoint = () => {
  if (points.value.length >= 2) {
    const p1 = points.value[points.value.length - 2];
    const p2 = points.value[points.value.length - 1];
    points.value.push({
      temperature: Math.round((p1.temperature + p2.temperature) / 2),
      fanSpeedPercent: Math.round((p1.fanSpeedPercent + p2.fanSpeedPercent) / 2)
    });
  } else {
    points.value.push({ temperature: 50, fanSpeedPercent: 50 });
  }
  sortPoints();
};

const removePoint = (index) => {
  if (points.value.length > 2) points.value.splice(index, 1);
};

onMounted(() => fetchCurve());
</script>

<style scoped>
.fan-curve-card {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.card-header {
  margin-bottom: 20px;
}

/* Обертка дает место для подписей X и Y */
.chart-outer {
  padding: 10px 20px 40px 45px; 
  background: rgba(0, 0, 0, 0.15);
  border: 1px solid var(--card-border);
  border-radius: 16px;
  margin-bottom: 24px;
}

.chart-inner {
  position: relative;
  width: 100%;
  height: 250px;
}

/* === HTML СЕТКА === */
.grid-lines {
  position: absolute;
  inset: 0;
  pointer-events: none;
}
.grid-line-h {
  position: absolute;
  left: 0; right: 0;
  height: 1px;
  border-bottom: 1px dashed rgba(255, 255, 255, 0.08);
}
.grid-line-v {
  position: absolute;
  top: 0; bottom: 0;
  width: 1px;
  border-left: 1px dashed rgba(255, 255, 255, 0.08);
}

/* === SVG === */
.curve-svg {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  overflow: visible;
  pointer-events: none;
}
.curve-fill { fill: url(#curveGradient); }
.curve-line {
  fill: none;
  stroke: var(--accent);
  stroke-width: 1.5;
  stroke-linejoin: round;
  stroke-linecap: round;
}

/* === HTML ТОЧКИ (Безупречные круги) === */
.html-point-wrapper {
  position: absolute;
  width: 32px;
  height: 32px;
  margin-left: -16px; /* Центровка */
  margin-bottom: -16px; /* Центровка */
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: grab;
  z-index: 10;
}
.html-point-wrapper:active { cursor: grabbing; }

.html-point-core {
  width: 12px;
  height: 12px;
  background: #ffffff;
  border: 2.5px solid var(--accent);
  border-radius: 50%;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.5);
  transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
}

/* Hover & Active Эффекты */
.html-point-wrapper:hover .html-point-core,
.html-point-wrapper.is-active .html-point-core {
  width: 16px;
  height: 16px;
  background: var(--accent);
  border-color: #ffffff;
  box-shadow: 0 0 12px var(--accent);
}

/* === ТУЛТИП === */
.html-tooltip {
  position: absolute;
  transform: translate(-50%, -24px); /* Поднимаем над точкой */
  background: rgba(30, 30, 46, 0.95);
  backdrop-filter: blur(8px);
  border: 1px solid rgba(255,255,255,0.1);
  color: #fff;
  padding: 6px 12px;
  border-radius: 8px;
  font-size: 0.8rem;
  font-weight: 600;
  pointer-events: none;
  box-shadow: 0 8px 16px rgba(0,0,0,0.4);
  white-space: nowrap;
  z-index: 20;
}
.divider { color: var(--text-muted); font-weight: 300; margin: 0 4px; }

/* === ПОДПИСИ ОСЕЙ === */
.axis-x-labels span {
  position: absolute;
  bottom: -28px;
  transform: translateX(-50%);
  font-size: 0.75rem;
  font-weight: 500;
  color: var(--text-muted);
}

.axis-y-labels span {
  position: absolute;
  left: -35px;
  transform: translateY(50%);
  font-size: 0.75rem;
  font-weight: 500;
  color: var(--text-muted);
}

/* === ПАНЕЛЬ УПРАВЛЕНИЯ === */
.controls-panel {
  display: flex;
  flex-direction: column;
  gap: 20px;
  margin-top: auto;
}

.points-list {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 12px;
}

.point-item {
  display: flex;
  align-items: center;
  background: rgba(255, 255, 255, 0.02);
  border: 1px solid var(--card-border);
  border-radius: 10px;
  padding: 10px 12px;
  gap: 12px;
  transition: background 0.2s;
}

.point-item:hover { background: rgba(255, 255, 255, 0.04); }

.point-badge {
  background: rgba(118, 185, 0, 0.15);
  color: var(--accent);
  width: 22px;
  height: 22px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  font-size: 0.75rem;
  font-weight: 700;
}

.input-group {
  display: flex;
  flex-direction: column;
  flex: 1;
}

.input-group label {
  font-size: 0.6rem;
  text-transform: uppercase;
  color: var(--text-muted);
  margin-bottom: 2px;
}

.input-group input {
  background: transparent;
  border: none;
  color: #fff;
  font-size: 0.95rem;
  font-weight: 500;
  width: 100%;
  outline: none;
  border-bottom: 1px solid transparent;
  transition: border-color 0.2s;
}

.input-group input:focus { border-bottom: 1px solid var(--accent); }
.actions-row { display: flex; gap: 15px; }
</style>