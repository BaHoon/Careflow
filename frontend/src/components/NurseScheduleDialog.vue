<template>
  <el-dialog
    v-model="dialogVisible"
    title="查看排班表"
    width="90%"
    :close-on-click-modal="false"
    @closed="handleClose"
  >
    <!-- 顶部筛选栏 -->
    <div class="schedule-header">
      <div class="filter-section">
        <!-- 病区选择 -->
        <el-select
          v-model="selectedWardId"
          placeholder="选择病区"
          clearable
          filterable
          @change="handleWardChange"
          style="width: 200px; margin-right: 12px;"
        >
          <el-option
            v-for="ward in wards"
            :key="ward.wardId"
            :label="`${ward.wardName} (${ward.departmentName})`"
            :value="ward.wardId"
          />
        </el-select>

        <!-- 视图切换 -->
        <el-radio-group 
          v-model="viewMode" 
          @change="handleViewModeChange"
          style="margin-right: 12px;"
        >
          <el-radio-button value="week">周视图</el-radio-button>
          <el-radio-button value="month">月视图</el-radio-button>
        </el-radio-group>

        <!-- 刷新按钮 -->
        <el-button 
          :icon="Refresh" 
          @click="loadScheduleData"
          :loading="loading"
        >
          刷新
        </el-button>
      </div>
    </div>

    <!-- 时间导航栏 -->
    <div class="time-navigation">
      <el-button 
        :icon="ArrowLeft" 
        @click="gotoPrevious"
        :disabled="loading"
      >
        {{ viewMode === 'week' ? '上一周' : '上一月' }}
      </el-button>
      
      <div class="time-range-display">
        <el-icon class="calendar-icon"><Calendar /></el-icon>
        <span class="range-text">{{ displayTimeRange }}</span>
      </div>
      
      <el-button 
        :icon="ArrowRight" 
        @click="gotoNext"
        :disabled="loading"
      >
        {{ viewMode === 'week' ? '下一周' : '下一月' }}
      </el-button>
      
      <el-button 
        @click="gotoToday"
        :disabled="loading"
        style="margin-left: 12px;"
      >
        今天
      </el-button>
    </div>

    <!-- 内容区域 -->
    <div class="schedule-content" v-loading="loading">
      <!-- 周视图 -->
      <div v-if="viewMode === 'week'" class="week-view">
        <el-empty 
          v-if="!loading && processedSchedules.length === 0" 
          description="暂无排班数据"
        />
        <div v-else class="week-table-container">
          <table class="week-table">
            <!-- 表头：日期 -->
            <thead>
              <tr>
                <th class="shift-column">班次</th>
                <th 
                  v-for="day in weekDays" 
                  :key="day.date"
                  :class="['day-column', { 'today': day.isToday }]"
                >
                  <div class="day-header">
                    <div class="weekday">{{ day.weekday }}</div>
                    <div class="date">{{ day.dateDisplay }}</div>
                  </div>
                </th>
              </tr>
            </thead>
            <!-- 表体：班次×日期 -->
            <tbody>
              <tr v-for="shift in shiftTypesBeijing" :key="shift.shiftId" class="shift-row">
                <!-- 班次名称列 -->
                <td class="shift-name-cell">
                  <div class="shift-info">
                    <div class="shift-name">{{ shift.shiftName }}</div>
                    <div class="shift-time">{{ shift.startTimeBeijing }} - {{ shift.endTimeBeijing }}</div>
                  </div>
                </td>
                <!-- 每天的排班 -->
                <td 
                  v-for="day in weekDays" 
                  :key="`${day.date}-${shift.shiftId}`"
                  class="schedule-cell"
                >
                  <div class="schedule-list">
                    <div
                      v-for="schedule in getSchedulesForDayAndShift(day.date, shift.shiftId)"
                      :key="schedule.id"
                      :class="['schedule-item', getStatusClass(schedule.status), { 'my-schedule': isMySchedule(schedule) }]"
                      @click="showScheduleDetail(schedule)"
                    >
                      <div class="nurse-name">{{ schedule.nurseName }}</div>
                      <div class="ward-name">{{ schedule.wardName }}</div>
                    </div>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- 月视图 -->
      <div v-else class="month-view">
        <el-empty 
          v-if="!loading && processedSchedules.length === 0" 
          description="暂无排班数据"
        />
        <div v-else class="month-calendar">
          <!-- 星期表头 -->
          <div class="calendar-weekdays">
            <div v-for="weekday in weekdayNames" :key="weekday" class="weekday-header">
              {{ weekday }}
            </div>
          </div>
          <!-- 日期网格 -->
          <div class="calendar-grid">
            <div
              v-for="day in calendarDays"
              :key="day.date"
              :class="[
                'calendar-day',
                {
                  'other-month': !day.isCurrentMonth,
                  'today': day.isToday,
                  'has-schedule': day.scheduleCount > 0,
                  'has-my-schedule': day.hasMySchedule
                }
              ]"
              @click="handleDayClick(day)"
            >
              <div class="day-number">{{ day.day }}</div>
              <div v-if="day.scheduleCount > 0" class="schedule-badge">
                {{ day.scheduleCount }}班
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 排班详情对话框 -->
    <el-dialog
      v-model="detailDialogVisible"
      title="排班详情"
      width="600px"
    >
      <div v-if="selectedSchedule" class="schedule-detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="护士姓名">
            {{ selectedSchedule.nurseName }}
          </el-descriptions-item>
          <el-descriptions-item label="工作日期">
            {{ selectedSchedule.beijingStartDate }}
          </el-descriptions-item>
          <el-descriptions-item label="病区">
            {{ selectedSchedule.wardName }}
          </el-descriptions-item>
          <el-descriptions-item label="科室">
            {{ selectedSchedule.departmentName }}
          </el-descriptions-item>
          <el-descriptions-item label="班次">
            {{ selectedSchedule.shiftName }}
          </el-descriptions-item>
          <el-descriptions-item label="时间">
            {{ selectedSchedule.displayRange }}
          </el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="getStatusTagType(selectedSchedule.status)">
              {{ getStatusText(selectedSchedule.status) }}
            </el-tag>
          </el-descriptions-item>
        </el-descriptions>
      </div>
    </el-dialog>

    <!-- 日期排班列表对话框 -->
    <el-dialog
      v-model="dayDetailDialogVisible"
      :title="`${selectedDay} 的排班`"
      width="800px"
    >
      <el-table :data="daySchedules" style="width: 100%" v-if="daySchedules.length > 0">
        <el-table-column prop="nurseName" label="护士" width="100">
          <template #default="{ row }">
            <span :class="{ 'my-schedule-highlight': isMySchedule(row) }">
              {{ row.nurseName }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="wardName" label="病区" width="120" />
        <el-table-column prop="shiftName" label="班次" width="100" />
        <el-table-column label="时间" width="200">
          <template #default="{ row }">
            {{ row.displayRange }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusTagType(row.status)" size="small">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="80">
          <template #default="{ row }">
            <el-button type="primary" link @click="showScheduleDetail(row)">
              详情
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-else description="该日期无排班记录" />
    </el-dialog>
  </el-dialog>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Refresh, ArrowLeft, ArrowRight, Calendar } from '@element-plus/icons-vue'
import { getScheduleList, getWards, getShiftTypes } from '../api/nurseSchedule'
import { 
  processScheduleData, 
  convertBeijingDateRangeToUTC,
  getWeekRange,
  getMonthRange
} from '../utils/scheduleTimeConverter'

// Props
const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false
  },
  // 当前用户信息
  userInfo: {
    type: Object,
    default: () => ({})
  }
})

// Emits
const emit = defineEmits(['update:modelValue'])

// 数据
const dialogVisible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

const loading = ref(false)
const viewMode = ref('week') // 'week' | 'month'
const currentDate = ref(new Date())
const selectedWardId = ref(null)

// 原始数据
const schedules = ref([])
const wards = ref([])
const shiftTypes = ref([])

// 处理后的数据（已转换时区）
const processedSchedules = computed(() => {
  return processScheduleData(schedules.value)
})

// 当前用户ID（userInfo 中存储的是 staffId）
const currentUserId = computed(() => {
  const userId = props.userInfo?.staffId || props.userInfo?.userId || props.userInfo?.id || null
  console.log('🔍 [NurseScheduleDialog] currentUserId computed:', userId, 'from userInfo:', props.userInfo)
  return userId
})

// 周视图：生成7天的数据
const weekDays = computed(() => {
  const range = getWeekRange(currentDate.value)
  const today = new Date().toISOString().split('T')[0]
  const weekdayNames = ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
  
  return range.weekDays.map((date, index) => {
    const dateObj = new Date(date)
    return {
      date,
      weekday: weekdayNames[index],
      dateDisplay: `${dateObj.getMonth() + 1}/${dateObj.getDate()}`,
      isToday: date === today
    }
  })
})

// 班次类型（转换为北京时间显示）
const shiftTypesBeijing = computed(() => {
  return shiftTypes.value.map(shift => {
    // 将 UTC 时间转换为北京时间显示
    // 注意：ShiftType 的时间是 TimeSpan，需要假设一个基准日期
    const parseTime = (timeStr) => {
      if (!timeStr) return '00:00'
      const parts = timeStr.split(':')
      const hours = parseInt(parts[0])
      const minutes = parseInt(parts[1])
      
      // 转换为北京时间（+8小时）
      let beijingHours = hours + 8
      if (beijingHours >= 24) beijingHours -= 24
      
      return `${String(beijingHours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`
    }
    
    return {
      ...shift,
      startTimeBeijing: parseTime(shift.startTime),
      endTimeBeijing: parseTime(shift.endTime)
    }
  })
})

// 月视图：生成日历数据
const calendarDays = computed(() => {
  const range = getMonthRange(currentDate.value)
  const year = range.year
  const month = range.month - 1 // JavaScript月份从0开始
  
  const firstDay = new Date(year, month, 1)
  const lastDay = new Date(year, month + 1, 0)
  const firstDayOfWeek = firstDay.getDay()
  const adjustedFirstDay = firstDayOfWeek === 0 ? 6 : firstDayOfWeek - 1 // 周一为0
  
  const days = []
  const today = new Date().toISOString().split('T')[0]
  
  // 上个月的日期
  const prevMonthLastDay = new Date(year, month, 0).getDate()
  for (let i = adjustedFirstDay - 1; i >= 0; i--) {
    const day = prevMonthLastDay - i
    const prevMonth = month === 0 ? 12 : month
    const prevYear = month === 0 ? year - 1 : year
    const date = `${prevYear}-${String(prevMonth).padStart(2, '0')}-${String(day).padStart(2, '0')}`
    const daySchedules = processedSchedules.value.filter(s => s.beijingStartDate === date)
    
    days.push({
      date,
      day,
      isCurrentMonth: false,
      isToday: false,
      scheduleCount: daySchedules.length,
      hasMySchedule: currentUserId.value && daySchedules.some(s => s.nurseId === currentUserId.value)
    })
  }
  
  // 当月的日期
  for (let day = 1; day <= lastDay.getDate(); day++) {
    const date = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`
    const daySchedules = processedSchedules.value.filter(s => s.beijingStartDate === date)
    const hasMySchedule = currentUserId.value && daySchedules.some(s => s.nurseId === currentUserId.value)
    
    if (hasMySchedule) {
      console.log('📅 [NurseScheduleDialog] Day with my schedule:', date, 'schedules:', daySchedules.map(s => `${s.nurseName}(${s.nurseId})`))
    }
    
    days.push({
      date,
      day,
      isCurrentMonth: true,
      isToday: date === today,
      scheduleCount: daySchedules.length,
      hasMySchedule
    })
  }
  
  // 下个月的日期（补齐到42天）
  const remainingDays = 42 - days.length
  const nextMonth = month === 11 ? 1 : month + 2
  const nextYear = month === 11 ? year + 1 : year
  for (let day = 1; day <= remainingDays; day++) {
    const date = `${nextYear}-${String(nextMonth).padStart(2, '0')}-${String(day).padStart(2, '0')}`
    const daySchedules = processedSchedules.value.filter(s => s.beijingStartDate === date)
    
    days.push({
      date,
      day,
      isCurrentMonth: false,
      isToday: false,
      scheduleCount: daySchedules.length,
      hasMySchedule: currentUserId.value && daySchedules.some(s => s.nurseId === currentUserId.value)
    })
  }
  
  return days
})

const weekdayNames = ['周一', '周二', '周三', '周四', '周五', '周六', '周日']

// 详情对话框
const detailDialogVisible = ref(false)
const selectedSchedule = ref(null)
const dayDetailDialogVisible = ref(false)
const selectedDay = ref('')
const daySchedules = ref([])

// 计算当前时间范围
const currentRange = computed(() => {
  if (viewMode.value === 'week') {
    return getWeekRange(currentDate.value)
  } else {
    return getMonthRange(currentDate.value)
  }
})

// 显示的时间范围文本
const displayTimeRange = computed(() => {
  const range = currentRange.value
  if (viewMode.value === 'week') {
    return `${range.startDate} 至 ${range.endDate}`
  } else {
    return `${range.year}年${range.month}月`
  }
})

// 方法
const handleClose = () => {
  // 关闭时清理状态（可选）
}

const handleWardChange = () => {
  loadScheduleData()
}

const handleViewModeChange = () => {
  loadScheduleData()
}

const gotoPrevious = () => {
  if (viewMode.value === 'week') {
    // 上一周
    const date = new Date(currentDate.value)
    date.setDate(date.getDate() - 7)
    currentDate.value = date
  } else {
    // 上一月
    const date = new Date(currentDate.value)
    date.setMonth(date.getMonth() - 1)
    currentDate.value = date
  }
  loadScheduleData()
}

const gotoNext = () => {
  if (viewMode.value === 'week') {
    // 下一周
    const date = new Date(currentDate.value)
    date.setDate(date.getDate() + 7)
    currentDate.value = date
  } else {
    // 下一月
    const date = new Date(currentDate.value)
    date.setMonth(date.getMonth() + 1)
    currentDate.value = date
  }
  loadScheduleData()
}

const gotoToday = () => {
  currentDate.value = new Date()
  loadScheduleData()
}

/**
 * 加载排班数据
 */
const loadScheduleData = async () => {
  try {
    loading.value = true

    // 获取当前显示范围的北京时间日期
    const range = currentRange.value
    
    // 转换为 UTC 日期范围（用于API查询）
    const utcRange = convertBeijingDateRangeToUTC(range.startDate, range.endDate)
    
    if (!utcRange) {
      throw new Error('日期范围转换失败')
    }

    // 获取用户的科室ID
    const departmentId = props.userInfo?.deptCode || null

    // 并行请求数据
    const [scheduleRes, wardRes, shiftRes] = await Promise.all([
      getScheduleList({
        startDate: utcRange.utcStartDate,
        endDate: utcRange.utcEndDate,
        wardId: selectedWardId.value || undefined,
        departmentId: departmentId || undefined
      }),
      getWards(departmentId),
      getShiftTypes()
    ])

    // 更新数据
    schedules.value = scheduleRes.schedules || []
    wards.value = wardRes || []
    shiftTypes.value = shiftRes || []

    console.log('✅ 排班数据加载成功:', {
      原始数据数量: schedules.value.length,
      病区数量: wards.value.length,
      班次类型数量: shiftTypes.value.length,
      UTC查询范围: utcRange,
      北京时间范围: range
    })

  } catch (error) {
    console.error('❌ 加载排班数据失败:', error)
    ElMessage.error('加载排班数据失败：' + (error.message || '未知错误'))
  } finally {
    loading.value = false
  }
}

/**
 * 初始化加载
 */
const initLoad = async () => {
  if (dialogVisible.value) {
    await loadScheduleData()
  }
}

/**
 * 获取指定日期和班次的排班列表
 */
const getSchedulesForDayAndShift = (date, shiftId) => {
  return processedSchedules.value.filter(s => 
    s.beijingStartDate === date && s.shiftId === shiftId
  )
}

/**
 * 判断是否是当前用户的排班
 */
const isMySchedule = (schedule) => {
  const result = currentUserId.value && schedule.nurseId === currentUserId.value
  if (result) {
    console.log('✅ [NurseScheduleDialog] Found my schedule:', schedule.nurseName, schedule.beijingStartDate, schedule.shiftName)
  }
  return result
}

/**
 * 获取状态样式类名
 */
const getStatusClass = (status) => {
  const statusMap = {
    'Scheduled': 'status-scheduled',
    'CheckedIn': 'status-checkedin',
    'Leave': 'status-leave'
  }
  return statusMap[status] || 'status-scheduled'
}

/**
 * 获取状态标签类型
 */
const getStatusTagType = (status) => {
  const typeMap = {
    'Scheduled': '',
    'CheckedIn': 'success',
    'Leave': 'warning'
  }
  return typeMap[status] || ''
}

/**
 * 获取状态文本
 */
const getStatusText = (status) => {
  const textMap = {
    'Scheduled': '已排班',
    'CheckedIn': '已签到',
    'Leave': '请假'
  }
  return textMap[status] || status
}

/**
 * 显示排班详情
 */
const showScheduleDetail = (schedule) => {
  selectedSchedule.value = schedule
  detailDialogVisible.value = true
}

/**
 * 处理日期点击（月视图）
 */
const handleDayClick = (day) => {
  if (day.scheduleCount === 0) return
  
  selectedDay.value = day.date
  daySchedules.value = processedSchedules.value.filter(s => 
    s.beijingStartDate === day.date
  )
  dayDetailDialogVisible.value = true
}

// 监听弹窗打开
watch(dialogVisible, (newVal) => {
  if (newVal) {
    initLoad()
  }
})

// 组件挂载时如果已打开则加载
onMounted(() => {
  if (dialogVisible.value) {
    initLoad()
  }
})
</script>

<style scoped>
.schedule-header {
  padding: 16px 0;
  border-bottom: 1px solid #e4e7ed;
  margin-bottom: 16px;
}

.filter-section {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}

.time-navigation {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px 0;
  gap: 12px;
  border-bottom: 1px solid #e4e7ed;
  margin-bottom: 20px;
}

.time-range-display {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 300px;
  justify-content: center;
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.calendar-icon {
  font-size: 20px;
  color: #409eff;
}

.range-text {
  user-select: none;
}

.schedule-content {
  min-height: 400px;
  padding: 20px 0;
}

/* ==================== 周视图表格 ==================== */
.week-view {
  width: 100%;
  overflow-x: auto;
}

.week-table-container {
  min-width: 900px;
}

.week-table {
  width: 100%;
  border-collapse: collapse;
  background: #fff;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
}

.week-table thead tr {
  background: #409eff;
}

.week-table th {
  padding: 16px 8px;
  text-align: center;
  color: #fff;
  font-weight: 600;
  border-right: 1px solid rgba(255, 255, 255, 0.2);
}

.week-table th:last-child {
  border-right: none;
}

.shift-column {
  width: 120px;
  min-width: 120px;
}

.day-column {
  width: calc((100% - 120px) / 7);
}

.day-column.today {
  background: #67c23a;
}

.day-header {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.day-header .weekday {
  font-size: 14px;
}

.day-header .date {
  font-size: 12px;
  opacity: 0.9;
}

.week-table tbody tr {
  border-bottom: 1px solid #e4e7ed;
}

.week-table tbody tr:last-child {
  border-bottom: none;
}

.week-table tbody tr:hover {
  background: #f5f7fa;
}

.shift-name-cell {
  padding: 12px;
  background: #fafafa;
  border-right: 2px solid #e4e7ed;
  vertical-align: middle;
}

.shift-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.shift-name {
  font-weight: 600;
  color: #303133;
  font-size: 14px;
}

.shift-time {
  font-size: 12px;
  color: #909399;
}

.schedule-cell {
  padding: 8px;
  vertical-align: top;
  border-right: 1px solid #e4e7ed;
  min-height: 80px;
}

.schedule-cell:last-child {
  border-right: none;
}

.schedule-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.schedule-item {
  padding: 8px;
  background: #ecf5ff;
  border: 1px solid #d9ecff;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.3s;
}

.schedule-item:hover {
  background: #d9ecff;
  transform: translateY(-2px);
  box-shadow: 0 2px 8px rgba(64, 158, 255, 0.2);
}

.schedule-item.my-schedule {
  background: #fef0e7;
  border-color: #e6a23c;
  border-width: 2px;
  font-weight: 600;
  box-shadow: 0 3px 10px rgba(230, 162, 60, 0.4);
}

.schedule-item.my-schedule .nurse-name {
  color: #e6a23c;
}

.schedule-item.my-schedule:hover {
  background: #fde8cf;
  transform: translateY(-2px);
  box-shadow: 0 4px 14px rgba(230, 162, 60, 0.5);
}

.schedule-item.status-scheduled {
  background: #ecf5ff;
  border-color: #d9ecff;
}

.schedule-item.status-checkedin {
  background: #f0f9ff;
  border-color: #b3e19d;
}

.schedule-item.status-leave {
  background: #fef0f0;
  border-color: #fbc4c4;
  opacity: 0.7;
}

.nurse-name {
  font-size: 13px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 2px;
}

.ward-name {
  font-size: 11px;
  color: #606266;
}

/* ==================== 月视图日历 ==================== */
.month-view {
  width: 100%;
}

.month-calendar {
  background: #fff;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
}

.calendar-weekdays {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 8px;
  margin-bottom: 12px;
}

.weekday-header {
  padding: 12px;
  text-align: center;
  font-weight: 600;
  color: #606266;
  background: #f5f7fa;
  border-radius: 4px;
}

.calendar-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 8px;
  width: 100%;
}

.calendar-day {
  min-height: 70px;
  height: 70px;
  padding: 8px 12px;
  border: 2px solid #e4e7ed;
  border-radius: 8px;
  display: flex;
  flex-direction: row;
  align-items: center;
  justify-content: space-between;
  cursor: pointer;
  transition: all 0.3s;
  background: #fff;
}

.calendar-day:hover {
  border-color: #409eff;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(64, 158, 255, 0.15);
}

.calendar-day.other-month {
  opacity: 0.3;
  background: #fafafa;
}

.calendar-day.today {
  background: #ecf5ff;
  color: #409eff;
  border-color: #409eff;
  border-width: 3px;
}

.calendar-day.today .day-number {
  color: #409eff;
  font-weight: 700;
  font-size: 24px;
}

.calendar-day.has-schedule {
  border-color: #409eff;
  background: #f0f9ff;
}

.calendar-day.has-my-schedule {
  background: #fef0e7;
  border-color: #e6a23c;
  border-width: 3px;
  font-weight: 600;
  box-shadow: 0 3px 12px rgba(230, 162, 60, 0.45);
}

.calendar-day.has-my-schedule:hover {
  background: #fde8cf;
  box-shadow: 0 4px 16px rgba(230, 162, 60, 0.55);
  transform: translateY(-2px);
}

.calendar-day.has-my-schedule .day-number {
  color: #e6a23c;
  font-weight: 700;
}

.day-number {
  font-size: 20px;
  font-weight: 600;
  color: #303133;
  flex-shrink: 0;
}

.schedule-badge {
  font-size: 12px;
  padding: 4px 10px;
  background: #409eff;
  color: #fff;
  border-radius: 12px;
  white-space: nowrap;
  flex-shrink: 0;
  font-weight: 600;
}

.calendar-day.has-my-schedule .schedule-badge {
  background: #e6a23c;
  font-weight: 700;
  box-shadow: 0 2px 4px rgba(230, 162, 60, 0.3);
}

/* ==================== 详情对话框 ==================== */
.schedule-detail {
  padding: 12px 0;
}

.my-schedule-highlight {
  color: #e6a23c;
  font-weight: 700;
}

/* ==================== 响应式 ==================== */
@media (max-width: 1200px) {
  .week-table-container {
    min-width: 100%;
  }
  
  .shift-column {
    width: 100px;
    min-width: 100px;
  }
  
  .nurse-name,
  .ward-name {
    font-size: 11px;
  }
  
  .calendar-day {
    min-height: 65px;
    height: 65px;
    padding: 6px 10px;
  }
  
  .day-number {
    font-size: 18px;
  }
  
  .schedule-badge {
    font-size: 11px;
    padding: 3px 8px;
  }
}

@media (max-width: 768px) {
  .filter-section {
    flex-direction: column;
    align-items: stretch;
  }

  .filter-section > * {
    width: 100% !important;
  }

  .time-navigation {
    flex-wrap: wrap;
  }

  .time-range-display {
    width: 100%;
    order: -1;
    margin-bottom: 12px;
  }
  
  .week-table {
    font-size: 12px;
  }
  
  .schedule-item {
    padding: 6px;
  }
  
  .calendar-day {
    min-height: 60px;
    height: 60px;
    padding: 4px 8px;
  }
  
  .day-number {
    font-size: 16px;
  }
  
  .schedule-badge {
    font-size: 10px;
    padding: 2px 6px;
  }
}
</style>
