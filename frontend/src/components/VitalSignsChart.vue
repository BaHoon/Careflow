<template>
  <el-dialog
    v-model="visible"
    title="近期体征监测"
    width="80%"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <div class="chart-container">
      <el-row :gutter="20" class="controls">
        <el-col :span="12">
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            @change="handleDateRangeChange"
          />
        </el-col>
        <el-col :span="12" style="text-align: right;">
          <el-button type="primary" @click="loadChartData" :loading="chartLoading">
            刷新数据
          </el-button>
        </el-col>
      </el-row>

      <el-empty 
        v-if="recordsLoaded && vitalSigns.length === 0"
        description="选定时期内无护理记录" 
        :image-size="100" 
      />
      
      <div v-else-if="!recordsLoaded && !chartLoading" style="text-align: center; color: #909399; padding: 40px 0;">
        <p>请选择日期范围，然后点击"刷新数据"查看体征趋势</p>
      </div>

      <el-skeleton v-if="chartLoading" :rows="5" animated />
      
      <template v-if="recordsLoaded && vitalSigns.length > 0">
        <!-- 表格 -->
        <div style="margin-bottom: 30px;">
          <h4>体征记录详情</h4>
          <el-table
            :data="vitalSigns"
            stripe
            style="width: 100%;"
            :default-sort="{ prop: 'recordTime', order: 'descending' }"
            size="small"
          >
            <el-table-column prop="recordTime" label="记录时间" min-width="150" sortable>
              <template #default="{ row }">
                {{ formatDateTime(row.recordTime) }}
              </template>
            </el-table-column>
            <el-table-column prop="temperature" label="体温(℃)" min-width="100" />
            <el-table-column prop="pulse" label="脉搏(次/分)" min-width="120" />
            <el-table-column prop="respiration" label="呼吸(次/分)" min-width="120" />
            <el-table-column label="血压(mmHg)" min-width="140">
              <template #default="{ row }">
                {{ row.sysBp && row.diaBp ? `${row.sysBp}/${row.diaBp}` : '-' }}
              </template>
            </el-table-column>
            <el-table-column prop="spo2" label="血氧(%)" min-width="100" />
          </el-table>
        </div>

        <!-- 统计信息 -->
        <el-row :gutter="20" style="margin-bottom: 30px;">
          <el-col :span="6">
            <div class="stat-card">
              <div class="stat-label">体温范围</div>
              <div class="stat-value">{{ statsData.tempRange }}</div>
            </div>
          </el-col>
          <el-col :span="6">
            <div class="stat-card">
              <div class="stat-label">脉搏范围</div>
              <div class="stat-value">{{ statsData.pulseRange }}</div>
            </div>
          </el-col>
          <el-col :span="6">
            <div class="stat-card">
              <div class="stat-label">血压范围</div>
              <div class="stat-value">{{ statsData.bpRange }}</div>
            </div>
          </el-col>
          <el-col :span="6">
            <div class="stat-card">
              <div class="stat-label">血氧范围</div>
              <div class="stat-value">{{ statsData.spo2Range }}</div>
            </div>
          </el-col>
        </el-row>

        <!-- 折线图 -->
        <h4 style="margin-top: 30px;">体征趋势图</h4>
        <el-row :gutter="20">
          <el-col :span="12">
            <div ref="temperatureChartRef" style="width: 100%; height: 350px; border: 1px solid #eee; border-radius: 4px;"></div>
          </el-col>
          <el-col :span="12">
            <div ref="pulseChartRef" style="width: 100%; height: 350px; border: 1px solid #eee; border-radius: 4px;"></div>
          </el-col>
        </el-row>

        <el-row :gutter="20" style="margin-top: 20px;">
          <el-col :span="12">
            <div ref="bpChartRef" style="width: 100%; height: 350px; border: 1px solid #eee; border-radius: 4px;"></div>
          </el-col>
          <el-col :span="12">
            <div ref="spo2ChartRef" style="width: 100%; height: 350px; border: 1px solid #eee; border-radius: 4px;"></div>
          </el-col>
        </el-row>
      </template>
    </div>
  </el-dialog>
</template>

<script setup>
import { ref, computed, watch, nextTick, onBeforeUnmount } from 'vue';
import { getVitalSignsHistory } from '@/api/nursing';
import { ElMessage } from 'element-plus';
import * as echarts from 'echarts';

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false
  },
  patientId: {
    type: String,
    default: ''
  }
});

const emit = defineEmits(['update:modelValue']);

const visible = ref(false);
const chartLoading = ref(false);
const recordsLoaded = ref(false);
const dateRange = ref([]);
const vitalSigns = ref([]);

// 图表实例
const temperatureChartRef = ref(null);
const pulseChartRef = ref(null);
const bpChartRef = ref(null);
const spo2ChartRef = ref(null);

let temperatureChart = null;
let pulseChart = null;
let bpChart = null;
let spo2Chart = null;

// 监听modelValue变化
watch(() => props.modelValue, (newVal) => {
  visible.value = newVal;
  if (newVal && dateRange.value.length === 0) {
    // 初始化默认日期范围：最近7天
    const endDate = new Date();
    const startDate = new Date(endDate.getTime() - 7 * 24 * 60 * 60 * 1000);
    dateRange.value = [startDate, endDate];
  }
});

const handleDateRangeChange = () => {
  if (dateRange.value.length === 2) {
    loadChartData();
  }
};

const loadChartData = async () => {
  if (!props.patientId || dateRange.value.length !== 2) {
    ElMessage.warning('请先选择日期范围');
    return;
  }

  try {
    chartLoading.value = true;
    recordsLoaded.value = false;
    vitalSigns.value = [];
    
    const startDate = dateRange.value[0];
    const endDate = dateRange.value[1];

    const response = await getVitalSignsHistory(
      props.patientId,
      startDate.toISOString().split('T')[0],
      endDate.toISOString().split('T')[0]
    );

    // 按时间从早到晚排序
    vitalSigns.value = (response || []).sort((a, b) => {
      return new Date(a.recordTime).getTime() - new Date(b.recordTime).getTime();
    });
    recordsLoaded.value = true;
    
    if (vitalSigns.value.length === 0) {
      ElMessage.info({ message: '该日期范围内没有护理记录', duration: 3000 });
    } else {
      //ElMessage.success(`加载了 ${vitalSigns.value.length} 条护理记录`);
      // 等待DOM更新后绘制图表
      await nextTick();
      setTimeout(() => {
        initCharts();
      }, 300);
    }
  } catch (error) {
    console.error('加载体征历史失败:', error);
    recordsLoaded.value = true;
    ElMessage.error('加载体征历史失败，请重试');
  } finally {
    chartLoading.value = false;
  }
};

// 初始化所有图表
const initCharts = () => {
  if (vitalSigns.value.length === 0) return;

  // 使用时间戳数据，让图表按真实时间间隔均分
  const timeData = vitalSigns.value.map(v => {
    const date = new Date(v.recordTime.endsWith('Z') ? v.recordTime : v.recordTime + 'Z');
    return date.getTime();
  });
  const timeLabels = vitalSigns.value.map(v => formatDateTime(v.recordTime));
  const temperatureData = vitalSigns.value.map(v => v.temperature);
  const pulseData = vitalSigns.value.map(v => v.pulse);
  const sysBpData = vitalSigns.value.map(v => v.sysBp);
  const diaBpData = vitalSigns.value.map(v => v.diaBp);
  const spo2Data = vitalSigns.value.map(v => v.spo2);

  initTemperatureChart(timeData, timeLabels, temperatureData);
  initPulseChart(timeData, timeLabels, pulseData);
  initBPChart(timeData, timeLabels, sysBpData, diaBpData);
  initSpo2Chart(timeData, timeLabels, spo2Data);

  // 监听窗口大小变化
  window.addEventListener('resize', handleResize);
};

const initTemperatureChart = (timeData, timeLabels, data) => {
  if (!temperatureChartRef.value) return;

  if (temperatureChart) {
    temperatureChart.dispose();
  }

  temperatureChart = echarts.init(temperatureChartRef.value);

  const option = {
    title: { text: '体温变化趋势', left: 'center', top: '2%' },
    tooltip: { 
      trigger: 'axis', 
      backgroundColor: 'rgba(0,0,0,0.8)', 
      borderColor: '#333',
      formatter: (params) => {
        if (params.length > 0) {
          const idx = params[0].dataIndex;
          return `${timeLabels[idx]}<br/>体温: ${params[0].value}℃`;
        }
        return '';
      }
    },
    grid: { top: '15%', left: '10%', right: '10%', bottom: '10%', containLabel: true },
    xAxis: { type: 'time', boundaryGap: false },
    yAxis: { type: 'value', name: '℃', min: 35, max: 42 },
    series: [
      {
        data: timeData.map((t, i) => [t, data[i]]),
        type: 'line',
        smooth: true,
        itemStyle: { color: '#FF6B6B' },
        symbolSize: 4,
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(255, 107, 107, 0.3)' },
            { offset: 1, color: 'rgba(255, 107, 107, 0)' }
          ])
        }
      }
    ]
  };

  temperatureChart.setOption(option);
};

const initPulseChart = (timeData, timeLabels, data) => {
  if (!pulseChartRef.value) return;

  if (pulseChart) {
    pulseChart.dispose();
  }

  pulseChart = echarts.init(pulseChartRef.value);

  const option = {
    title: { text: '脉搏变化趋势', left: 'center', top: '2%' },
    tooltip: { 
      trigger: 'axis', 
      backgroundColor: 'rgba(0,0,0,0.8)', 
      borderColor: '#333',
      formatter: (params) => {
        if (params.length > 0) {
          const idx = params[0].dataIndex;
          return `${timeLabels[idx]}<br/>脉搏: ${params[0].value}次/分`;
        }
        return '';
      }
    },
    grid: { top: '15%', left: '10%', right: '10%', bottom: '10%', containLabel: true },
    xAxis: { type: 'time', boundaryGap: false },
    yAxis: { type: 'value', name: '次/分' },
    series: [
      {
        data: timeData.map((t, i) => [t, data[i]]),
        type: 'line',
        smooth: true,
        itemStyle: { color: '#4ECDC4' },
        symbolSize: 4,
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(78, 205, 196, 0.3)' },
            { offset: 1, color: 'rgba(78, 205, 196, 0)' }
          ])
        }
      }
    ]
  };

  pulseChart.setOption(option);
};

const initBPChart = (timeData, timeLabels, sysBpData, diaBpData) => {
  if (!bpChartRef.value) return;

  if (bpChart) {
    bpChart.dispose();
  }

  bpChart = echarts.init(bpChartRef.value);

  const option = {
    title: { text: '血压变化趋势', left: 'center', top: '2%' },
    tooltip: { 
      trigger: 'axis', 
      backgroundColor: 'rgba(0,0,0,0.8)', 
      borderColor: '#333',
      formatter: (params) => {
        if (params.length > 0) {
          const idx = params[0].dataIndex;
          const sys = params.find(p => p.seriesName === '收缩压')?.value || '-';
          const dia = params.find(p => p.seriesName === '舒张压')?.value || '-';
          return `${timeLabels[idx]}<br/>收缩压: ${sys}mmHg<br/>舒张压: ${dia}mmHg`;
        }
        return '';
      }
    },
    grid: { top: '15%', left: '10%', right: '10%', bottom: '10%', containLabel: true },
    xAxis: { type: 'time', boundaryGap: false },
    yAxis: { type: 'value', name: 'mmHg' },
    legend: { data: ['收缩压', '舒张压'], top: '8%' },
    series: [
      {
        name: '收缩压',
        data: timeData.map((t, i) => [t, sysBpData[i]]),
        type: 'line',
        smooth: true,
        itemStyle: { color: '#F38181' },
        symbolSize: 4,
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(243, 129, 129, 0.2)' },
            { offset: 1, color: 'rgba(243, 129, 129, 0)' }
          ])
        }
      },
      {
        name: '舒张压',
        data: timeData.map((t, i) => [t, diaBpData[i]]),
        type: 'line',
        smooth: true,
        itemStyle: { color: '#AA96DA' },
        symbolSize: 4,
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(170, 150, 218, 0.2)' },
            { offset: 1, color: 'rgba(170, 150, 218, 0)' }
          ])
        }
      }
    ]
  };

  bpChart.setOption(option);
};

const initSpo2Chart = (timeData, timeLabels, data) => {
  if (!spo2ChartRef.value) return;

  if (spo2Chart) {
    spo2Chart.dispose();
  }

  spo2Chart = echarts.init(spo2ChartRef.value);

  const option = {
    title: { text: '血氧饱和度变化趋势', left: 'center', top: '2%' },
    tooltip: { 
      trigger: 'axis', 
      backgroundColor: 'rgba(0,0,0,0.8)', 
      borderColor: '#333',
      formatter: (params) => {
        if (params.length > 0) {
          const idx = params[0].dataIndex;
          return `${timeLabels[idx]}<br/>血氧: ${params[0].value}%`;
        }
        return '';
      }
    },
    grid: { top: '15%', left: '10%', right: '10%', bottom: '10%', containLabel: true },
    xAxis: { type: 'time', boundaryGap: false },
    yAxis: { type: 'value', name: '%', min: 90, max: 100 },
    series: [
      {
        data: timeData.map((t, i) => [t, data[i]]),
        type: 'line',
        smooth: true,
        itemStyle: { color: '#FCBAD3' },
        symbolSize: 4,
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(252, 186, 211, 0.3)' },
            { offset: 1, color: 'rgba(252, 186, 211, 0)' }
          ])
        }
      }
    ]
  };

  spo2Chart.setOption(option);
};

const handleResize = () => {
  temperatureChart?.resize();
  pulseChart?.resize();
  bpChart?.resize();
  spo2Chart?.resize();
};

const getTemperatureType = (temp) => {
  if (!temp) return '';
  if (temp < 36.5) return 'info';
  if (temp >= 36.5 && temp <= 37.5) return 'success';
  if (temp > 37.5 && temp < 38.5) return 'warning';
  return 'danger';
};

const formatDateTime = (datetime) => {
  if (!datetime) return '';
  try {
    let utcString = datetime;
    if (typeof datetime === 'string' && !datetime.endsWith('Z') && !datetime.includes('+')) {
      utcString = datetime + 'Z';
    }
    const date = new Date(utcString);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${month}-${day} ${hours}:${minutes}`;
  } catch {
    return datetime;
  }
};

// 计算统计数据
const statsData = computed(() => {
  if (vitalSigns.value.length === 0) {
    return {
      tempRange: '-',
      pulseRange: '-',
      bpRange: '-',
      spo2Range: '-'
    };
  }

  const temps = vitalSigns.value.map(v => v.temperature).filter(t => t);
  const pulses = vitalSigns.value.map(v => v.pulse).filter(p => p);
  const sysBps = vitalSigns.value.map(v => v.sysBp).filter(b => b);
  const diaBps = vitalSigns.value.map(v => v.diaBp).filter(b => b);
  const spo2s = vitalSigns.value.map(v => v.spo2).filter(s => s);

  return {
    tempRange: temps.length > 0 ? `${Math.min(...temps).toFixed(1)}~${Math.max(...temps).toFixed(1)}℃` : '-',
    pulseRange: pulses.length > 0 ? `${Math.min(...pulses)}~${Math.max(...pulses)}次/分` : '-',
    bpRange: sysBps.length > 0 ? `${Math.min(...sysBps)}~${Math.max(...sysBps)}/${Math.min(...diaBps)}~${Math.max(...diaBps)}mmHg` : '-',
    spo2Range: spo2s.length > 0 ? `${Math.min(...spo2s).toFixed(1)}~${Math.max(...spo2s).toFixed(1)}%` : '-'
  };
});

const handleClose = () => {
  emit('update:modelValue', false);
  // 清理图表
  window.removeEventListener('resize', handleResize);
};

// 组件卸载时清理
onBeforeUnmount(() => {
  temperatureChart?.dispose();
  pulseChart?.dispose();
  bpChart?.dispose();
  spo2Chart?.dispose();
  window.removeEventListener('resize', handleResize);
});
</script>

<style scoped>
.chart-container {
  padding: 20px;
  transform: scale(0.8);
  transform-origin: top left;
  width: 125%;
  box-sizing: border-box;
}

.controls {
  margin-bottom: 20px;
}

.stat-card {
  padding: 16px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 8px;
  color: white;
  text-align: center;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.stat-card:nth-child(2) {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
}

.stat-card:nth-child(3) {
  background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
}

.stat-card:nth-child(4) {
  background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%);
}

.stat-label {
  font-size: 12px;
  opacity: 0.9;
  margin-bottom: 8px;
}

.stat-value {
  font-size: 18px;
  font-weight: bold;
}

h4 {
  margin: 20px 0 15px 0;
  font-size: 16px;
  font-weight: 600;
  color: #333;
}

:deep(.el-dialog__body) {
  max-height: calc(100vh - 200px);
  overflow-y: auto;
}

:deep(.el-table) {
  font-size: 13px;
}

:deep(.el-table th.el-table__cell) {
  background-color: #f5f7fa;
  padding: 12px 0 !important;
}

:deep(.el-table td.el-table__cell) {
  padding: 12px 20px !important;
}

:deep(.el-table__body tr) {
  height: auto;
}
/* 异常值样式 */
.abnormal-value {
  background-color: #fef0f0;
  color: #f56c6c;
  font-weight: 600;
  padding: 2px 4px;
  border-radius: 2px;
  border-left: 3px solid #f56c6c;
  padding-left: 6px;
}
</style>

