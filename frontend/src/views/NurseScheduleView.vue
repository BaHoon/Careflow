<template>
  <div class="schedule-view">
    <!-- 顶部工具栏 -->
    <div class="schedule-toolbar">
      <div class="toolbar-left">
        <el-date-picker
          v-model="selectedDate"
          type="date"
          placeholder="选择日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          @change="handleDateChange"
          style="width: 200px; margin-right: 16px;"
        />
        <el-select
          v-model="selectedWardId"
          placeholder="全部病区"
          clearable
          @change="loadScheduleData"
          style="width: 180px; margin-right: 16px;"
        >
          <el-option
            v-for="ward in wards"
            :key="ward.wardId"
            :label="ward.wardName"
            :value="ward.wardId"
          />
        </el-select>
        <el-button @click="loadScheduleData" :icon="Refresh" type="primary">
          刷新
        </el-button>
      </div>
      <div class="toolbar-right">
        <el-radio-group v-model="viewMode" @change="handleViewModeChange">
          <el-radio-button label="week">周视图</el-radio-button>
          <el-radio-button label="month">月视图</el-radio-button>
        </el-radio-group>
      </div>
    </div>

    <!-- 日历主体 -->
    <div class="schedule-content">
      <!-- 周视图 -->
      <div v-if="viewMode === 'week'" class="week-view">
        <div class="week-header">
          <div class="time-column">时间</div>
          <div
            v-for="day in weekDays"
            :key="day.date"
            :class="['day-column', { today: day.isToday }]"
          >
            <div class="day-label">{{ day.label }}</div>
            <div class="day-date">{{ day.date }}</div>
          </div>
        </div>
        <div class="week-body">
          <div class="time-slot" v-for="shift in shiftTypes" :key="shift.shiftId">
            <div class="time-label">
              {{ shift.startTime }} - {{ shift.endTime }}
              <span class="shift-name">{{ shift.shiftName }}</span>
            </div>
            <div
              v-for="day in weekDays"
              :key="`${day.date}-${shift.shiftId}`"
              class="schedule-cell"
            >
              <div
                v-for="schedule in getSchedulesForDayAndShift(day.date, shift.shiftId)"
                :key="schedule.id"
                :class="['schedule-item', getStatusClass(schedule.status), { 'my-schedule': isMySchedule(schedule) }]"
                @click="showScheduleDetail(schedule)"
              >
                <div class="schedule-content-text">
                  <span class="nurse-name">{{ schedule.nurseName }}</span>
                  <span class="ward-name">{{ schedule.wardName }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 月视图 -->
      <div v-else class="month-view">
        <div class="calendar-header">
          <el-button @click="prevMonth" :icon="ArrowLeft" circle />
          <span class="month-title">{{ currentMonthText }}</span>
          <el-button @click="nextMonth" :icon="ArrowRight" circle />
        </div>
        <div class="calendar-grid">
          <div class="weekday-header">
            <div v-for="weekday in weekdays" :key="weekday" class="weekday">
              {{ weekday }}
            </div>
          </div>
          <div class="calendar-days">
            <div
              v-for="day in calendarDays"
              :key="day.date"
              :class="['calendar-day', { 
                'other-month': !day.isCurrentMonth,
                'today': day.isToday,
                'has-schedule': day.scheduleCount > 0,
                'has-my-schedule': day.hasMySchedule
              }]"
              @click="selectDay(day)"
            >
              <div class="day-number">{{ day.day }}</div>
              <div v-if="day.scheduleCount > 0" class="schedule-badge">
                {{ day.scheduleCount }}个排班
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
            {{ selectedSchedule.workDate }}
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
            {{ selectedSchedule.startTime }} - {{ selectedSchedule.endTime }}
          </el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="getStatusTagType(selectedSchedule.status)">
              {{ getStatusText(selectedSchedule.status) }}
            </el-tag>
          </el-descriptions-item>
        </el-descriptions>
      </div>
    </el-dialog>

    <!-- 日期详情对话框（月视图点击日期时显示） -->
    <el-dialog
      v-model="dayDetailDialogVisible"
      :title="`${selectedDayDate} 的排班`"
      width="800px"
    >
      <div v-if="daySchedules.length > 0">
          <el-table :data="daySchedules" style="width: 100%">
          <el-table-column prop="nurseName" label="护士" width="120">
            <template #default="{ row }">
              <span :class="{ 'my-schedule-text': isMySchedule(row) }">
                {{ row.nurseName }}
              </span>
            </template>
          </el-table-column>
          <el-table-column prop="wardName" label="病区" width="120" />
          <el-table-column prop="shiftName" label="班次" width="100" />
          <el-table-column label="时间" width="150">
            <template #default="{ row }">
              {{ row.startTime }} - {{ row.endTime }}
            </template>
          </el-table-column>
          <el-table-column prop="status" label="状态" width="100">
            <template #default="{ row }">
              <el-tag :type="getStatusTagType(row.status)" size="small">
                {{ getStatusText(row.status) }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="100">
            <template #default="{ row }">
              <el-button
                type="primary"
                link
                @click="showScheduleDetail(row)"
              >
                查看详情
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
      <el-empty v-else description="该日期无排班记录" />
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Refresh, ArrowLeft, ArrowRight } from '@element-plus/icons-vue'
import { getScheduleList, getWards, getShiftTypes } from '../api/nurseSchedule'

// 数据
const schedules = ref([])
const wards = ref([])
const shiftTypes = ref([])
const selectedDate = ref(new Date().toISOString().split('T')[0])
const selectedWardId = ref(null)
const viewMode = ref('week')
const detailDialogVisible = ref(false)
const dayDetailDialogVisible = ref(false)
const selectedSchedule = ref(null)
const selectedDayDate = ref('')
const daySchedules = ref([])
const currentNurseId = ref(null)
const currentDeptCode = ref(null)

// 计算属性
const currentMonthText = computed(() => {
  const date = new Date(selectedDate.value)
  return `${date.getFullYear()}年${date.getMonth() + 1}月`
})

const weekDays = computed(() => {
  const date = new Date(selectedDate.value)
  const dayOfWeek = date.getDay()
  const startOfWeek = new Date(date)
  startOfWeek.setDate(date.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1)) // 周一为起始

  const days = []
  const today = new Date().toISOString().split('T')[0]
  
  for (let i = 0; i < 7; i++) {
    const day = new Date(startOfWeek)
    day.setDate(startOfWeek.getDate() + i)
    const dateStr = day.toISOString().split('T')[0]
    const isToday = dateStr === today
    
    days.push({
      date: dateStr,
      label: ['周一', '周二', '周三', '周四', '周五', '周六', '周日'][i],
      isToday
    })
  }
  
  return days
})

const weekdays = ['日', '一', '二', '三', '四', '五', '六']

const calendarDays = computed(() => {
  const date = new Date(selectedDate.value)
  const year = date.getFullYear()
  const month = date.getMonth()
  
  // 获取当月第一天和最后一天
  const firstDay = new Date(year, month, 1)
  const lastDay = new Date(year, month + 1, 0)
  
  // 获取第一天是星期几（0=周日）
  const firstDayOfWeek = firstDay.getDay()
  
  // 获取上个月的最后几天
  const prevMonthLastDay = new Date(year, month, 0).getDate()
  
  const days = []
  const today = new Date().toISOString().split('T')[0]
  
  // 上个月的日期
  for (let i = firstDayOfWeek - 1; i >= 0; i--) {
    const day = prevMonthLastDay - i
    const prevMonth = month === 0 ? 12 : month
    const prevYear = month === 0 ? year - 1 : year
    const dateStr = `${prevYear}-${String(prevMonth).padStart(2, '0')}-${String(day).padStart(2, '0')}`
    const daySchedules = schedules.value.filter(s => s.workDate === dateStr)
    const hasMySchedule = currentNurseId.value && daySchedules.some(s => s.nurseId === currentNurseId.value)
    days.push({
      date: dateStr,
      day,
      isCurrentMonth: false,
      isToday: false,
      scheduleCount: daySchedules.length,
      hasMySchedule
    })
  }
  
  // 当月的日期
  for (let day = 1; day <= lastDay.getDate(); day++) {
    const dateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`
    const daySchedules = schedules.value.filter(s => s.workDate === dateStr)
    const hasMySchedule = currentNurseId.value && daySchedules.some(s => s.nurseId === currentNurseId.value)
    days.push({
      date: dateStr,
      day,
      isCurrentMonth: true,
      isToday: dateStr === today,
      scheduleCount: daySchedules.length,
      hasMySchedule
    })
  }
  
  // 下个月的日期（补齐到42天，6行×7列）
  const remainingDays = 42 - days.length
  const nextMonth = month === 11 ? 1 : month + 2
  const nextYear = month === 11 ? year + 1 : year
  for (let day = 1; day <= remainingDays; day++) {
    const dateStr = `${nextYear}-${String(nextMonth).padStart(2, '0')}-${String(day).padStart(2, '0')}`
    const daySchedules = schedules.value.filter(s => s.workDate === dateStr)
    const hasMySchedule = currentNurseId.value && daySchedules.some(s => s.nurseId === currentNurseId.value)
    days.push({
      date: dateStr,
      day,
      isCurrentMonth: false,
      isToday: false,
      scheduleCount: daySchedules.length,
      hasMySchedule
    })
  }
  
  return days
})

// 方法
const loadScheduleData = async () => {
  try {
    const date = new Date(selectedDate.value)
    const startDate = viewMode.value === 'week' 
      ? weekDays.value[0].date 
      : new Date(date.getFullYear(), date.getMonth(), 1).toISOString().split('T')[0]
    
    const endDate = viewMode.value === 'week'
      ? weekDays.value[6].date
      : new Date(date.getFullYear(), date.getMonth() + 1, 0).toISOString().split('T')[0]

    const [scheduleRes, wardRes, shiftRes] = await Promise.all([
      getScheduleList({
        startDate,
        endDate,
        wardId: selectedWardId.value || undefined,
        departmentId: currentDeptCode.value || undefined
      }),
      getWards(currentDeptCode.value || null),
      getShiftTypes()
    ])

    schedules.value = scheduleRes.schedules || []
    wards.value = wardRes || []
    shiftTypes.value = shiftRes || []
    
    // 调试信息
    console.log('加载的排班数据总数:', schedules.value.length)
    if (schedules.value.length > 0) {
      console.log('排班数据示例（第一个）:', schedules.value[0])
      console.log('排班数据字段:', Object.keys(schedules.value[0]))
    }
    if (currentNurseId.value) {
      const mySchedules = schedules.value.filter(s => s.nurseId === currentNurseId.value)
      console.log('当前护士ID:', currentNurseId.value)
      console.log('找到自己的排班数量:', mySchedules.length)
      if (mySchedules.length > 0) {
        console.log('自己的排班示例:', mySchedules[0])
      } else {
        console.warn('未找到自己的排班，请检查 nurseId 字段是否匹配')
        // 显示所有排班的 nurseId 以便调试
        const allNurseIds = [...new Set(schedules.value.map(s => s.nurseId))]
        console.log('所有排班中的 nurseId 列表:', allNurseIds)
      }
    } else {
      console.warn('当前护士ID为空，无法标识自己的排班')
    }
  } catch (error) {
    console.error('加载排班数据失败:', error)
    ElMessage.error('加载排班数据失败')
  }
}

const getSchedulesForDayAndShift = (date, shiftId) => {
  return schedules.value.filter(
    s => s.workDate === date && s.shiftId === shiftId
  )
}

const getScheduleCountForDate = (date) => {
  return schedules.value.filter(s => s.workDate === date).length
}

const isMySchedule = (schedule) => {
  if (!currentNurseId.value || !schedule) {
    return false
  }
  // 排班数据中的 nurseId 字段应该与当前登录的 staffId 匹配
  const isMatch = schedule.nurseId === currentNurseId.value
  return isMatch
}

const getStatusClass = (status) => {
  const statusMap = {
    'Scheduled': 'status-scheduled',
    'CheckedIn': 'status-checked-in',
    'Leave': 'status-leave'
  }
  return statusMap[status] || 'status-default'
}

const getStatusText = (status) => {
  const statusMap = {
    'Scheduled': '已排班',
    'CheckedIn': '已签到',
    'Leave': '请假'
  }
  return statusMap[status] || status
}

const getStatusTagType = (status) => {
  const typeMap = {
    'Scheduled': 'primary',
    'CheckedIn': 'success',
    'Leave': 'warning'
  }
  return typeMap[status] || 'info'
}

const showScheduleDetail = (schedule) => {
  selectedSchedule.value = schedule
  detailDialogVisible.value = true
}

const selectDay = (day) => {
  selectedDayDate.value = day.date
  daySchedules.value = schedules.value.filter(s => s.workDate === day.date)
  dayDetailDialogVisible.value = true
}

const handleDateChange = () => {
  loadScheduleData()
}

const handleViewModeChange = () => {
  loadScheduleData()
}

const prevMonth = () => {
  const date = new Date(selectedDate.value)
  date.setMonth(date.getMonth() - 1)
  selectedDate.value = date.toISOString().split('T')[0]
  loadScheduleData()
}

const nextMonth = () => {
  const date = new Date(selectedDate.value)
  date.setMonth(date.getMonth() + 1)
  selectedDate.value = date.toISOString().split('T')[0]
  loadScheduleData()
}

// 生命周期
onMounted(() => {
  // 获取当前登录护士信息
  const userInfo = localStorage.getItem('userInfo')
  if (userInfo) {
    try {
      const parsed = JSON.parse(userInfo)
      // 从登录信息中获取 staffId（这是护士的ID）和 deptCode（科室代码）
      currentNurseId.value = parsed.staffId || null
      currentDeptCode.value = parsed.deptCode || null
      console.log('当前用户信息:', parsed)
      console.log('当前护士ID:', currentNurseId.value)
      console.log('当前科室代码:', currentDeptCode.value)
    } catch (error) {
      console.error('解析用户信息失败:', error)
    }
  } else {
    console.warn('未找到用户信息')
  }
  loadScheduleData()
})
</script>

<style scoped>
.schedule-view {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 60px);
  padding: 24px;
  background: #f5f7fa;
  overflow: hidden;
}

/* 工具栏 */
.schedule-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  padding: 16px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.toolbar-left {
  display: flex;
  align-items: center;
}

.toolbar-right {
  display: flex;
  align-items: center;
}

/* 内容区域 */
.schedule-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  min-height: 0;
}

/* 周视图 */
.week-view {
  flex: 1;
  display: flex;
  flex-direction: column;
  width: 100%;
  min-height: 0;
  overflow: hidden;
}

.week-header {
  display: grid;
  grid-template-columns: 120px repeat(7, 1fr);
  background: #f5f7fa;
  border-bottom: 2px solid #e4e7ed;
  flex-shrink: 0;
}

.time-column {
  padding: 16px;
  font-weight: 600;
  text-align: center;
  border-right: 1px solid #e4e7ed;
}

.day-column {
  padding: 16px;
  text-align: center;
  border-right: 1px solid #e4e7ed;
}

.day-column.today {
  background: #ecf5ff;
  color: #409eff;
  font-weight: 600;
}

.day-label {
  font-size: 14px;
  font-weight: 600;
  margin-bottom: 4px;
}

.day-date {
  font-size: 12px;
  color: #909399;
}

.week-body {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  min-height: 0;
}

.time-slot {
  display: grid;
  grid-template-columns: 120px repeat(7, 1fr);
  border-bottom: 1px solid #e4e7ed;
  min-height: 100px;
}

.time-label {
  padding: 16px;
  text-align: center;
  border-right: 1px solid #e4e7ed;
  background: #fafafa;
  font-size: 13px;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.shift-name {
  font-size: 12px;
  color: #909399;
  margin-top: 4px;
}

.schedule-cell {
  padding: 8px;
  border-right: 1px solid #e4e7ed;
  min-height: 100px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.schedule-item {
  padding: 8px;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.3s;
  border: 1px solid #e4e7ed;
  min-height: 50px;
  display: flex;
  align-items: center;
}

.schedule-item:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.status-scheduled {
  background: #ecf5ff;
  border-color: #b3d8ff;
}

.status-checked-in {
  background: #f0f9ff;
  border-color: #b3d8ff;
}

.status-leave {
  background: #fdf6ec;
  border-color: #f5dab1;
}

/* 自己的排班特殊样式 */
.schedule-item.my-schedule {
  background: #fff7e6;
  border-color: #ffd591;
  border-width: 2px;
  box-shadow: 0 2px 4px rgba(255, 213, 145, 0.3);
}

.schedule-item.my-schedule.status-scheduled {
  background: #fff7e6;
  border-color: #ffd591;
}

.schedule-item.my-schedule.status-checked-in {
  background: #f6ffed;
  border-color: #b7eb8f;
}

.schedule-item.my-schedule.status-leave {
  background: #fff1f0;
  border-color: #ffccc7;
}

.schedule-content-text {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.nurse-name {
  font-weight: 600;
  font-size: 14px;
  color: #303133;
}

.schedule-item.my-schedule .nurse-name {
  color: #fa8c16;
  font-weight: 700;
}

.ward-name {
  font-size: 14px;
  color: #606266;
}

.schedule-item.my-schedule .ward-name {
  color: #fa8c16;
}

/* 月视图 */
.month-view {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 24px;
  min-height: 0;
  overflow: hidden;
}

.calendar-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  flex-shrink: 0;
}

.month-title {
  font-size: 20px;
  font-weight: 600;
  color: #303133;
}

.calendar-grid {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: hidden;
}

.weekday-header {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  background: #f5f7fa;
  border-bottom: 2px solid #e4e7ed;
  flex-shrink: 0;
}

.weekday {
  padding: 12px;
  text-align: center;
  font-weight: 600;
  color: #606266;
}

.calendar-days {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  grid-auto-rows: minmax(120px, 1fr);
  border: 1px solid #e4e7ed;
  border-top: none;
  overflow-y: auto;
  min-height: 0;
}

.calendar-day {
  min-height: 100px;
  padding: 8px;
  border-right: 1px solid #e4e7ed;
  border-bottom: 1px solid #e4e7ed;
  cursor: pointer;
  transition: all 0.3s;
  background: white;
}

.calendar-day:hover {
  background: #f5f7fa;
}

.calendar-day.other-month {
  background: #fafafa;
  color: #c0c4cc;
}

.calendar-day.today {
  background: #ecf5ff;
  border: 2px solid #409eff;
}

.calendar-day.has-schedule {
  background: #f0f9ff;
}

.calendar-day.has-my-schedule {
  background: #fff7e6;
  border: 2px solid #ffd591;
  box-shadow: inset 0 0 0 1px rgba(255, 213, 145, 0.3);
}

.calendar-day.has-my-schedule.today {
  background: #fff7e6;
  border: 2px solid #409eff;
  box-shadow: inset 0 0 0 1px rgba(255, 213, 145, 0.3);
}

.day-number {
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 4px;
}

.schedule-badge {
  font-size: 12px;
  color: #409eff;
  background: #ecf5ff;
  padding: 2px 6px;
  border-radius: 4px;
  display: inline-block;
  margin-top: 4px;
}

/* 详情对话框 */
.schedule-detail {
  padding: 16px 0;
}

/* 表格中自己的排班文字样式 */
.my-schedule-text {
  color: #fa8c16;
  font-weight: 700;
}

/* 滚动条美化 */
.week-body::-webkit-scrollbar,
.calendar-days::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}

.week-body::-webkit-scrollbar-track,
.calendar-days::-webkit-scrollbar-track {
  background: #f1f1f1;
  border-radius: 4px;
}

.week-body::-webkit-scrollbar-thumb,
.calendar-days::-webkit-scrollbar-thumb {
  background: #c1c1c1;
  border-radius: 4px;
}

.week-body::-webkit-scrollbar-thumb:hover,
.calendar-days::-webkit-scrollbar-thumb:hover {
  background: #a8a8a8;
}
</style>

