/**
 * 排班时间转换工具
 * 处理后端 UTC 时间与前端北京时间（UTC+8）的转换
 * 
 * 核心原则：
 * 1. 后端存储：WorkDate (DateOnly, UTC) + StartTime/EndTime (TimeSpan)
 * 2. 前端显示：必须将 WorkDate + TimeSpan 组合后整体转换，不能分离转换
 * 3. 跨天处理：夜班（如22:00-06:00）需要特殊处理结束日期
 * 
 * 参考 NurseScheduleRepository.GetNurseOnDutyAsync 的逻辑
 */

/**
 * 将后端的 UTC WorkDate + TimeSpan 转换为北京时间的日期+时间
 * 
 * @param {string} workDate - UTC日期，格式: "2025-12-28"
 * @param {string} timeSpan - 时间段，格式: "08:00" 或 "08:00:00"
 * @returns {Object} 转换后的北京时间对象
 * @returns {string} returns.date - 北京时间日期 "2025-12-28"
 * @returns {string} returns.time - 北京时间时间 "16:00"
 * @returns {Date} returns.dateTime - JavaScript Date 对象
 * @returns {string} returns.display - 显示格式 "2025-12-28 16:00"
 * 
 * @example
 * // UTC 2025-12-27 23:00 → 北京 2025-12-28 07:00 (早餐前)
 * convertUTCScheduleToBeijing('2025-12-27', '23:00')
 * // => { date: '2025-12-28', time: '07:00', ... }
 */
export function convertUTCScheduleToBeijing(workDate, timeSpan) {
  if (!workDate || !timeSpan) {
    return null;
  }

  try {
    // 1. 解析时间段（可能是 "HH:MM" 或 "HH:MM:SS"）
    const timeParts = timeSpan.split(':').map(Number);
    const hours = timeParts[0] || 0;
    const minutes = timeParts[1] || 0;
    const seconds = timeParts[2] || 0;

    // 2. 构造 UTC 日期时间（使用 'Z' 标识 UTC 时区）
    const utcDateTimeStr = `${workDate}T${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}Z`;
    const utcDateTime = new Date(utcDateTimeStr);

    // 验证日期是否有效
    if (isNaN(utcDateTime.getTime())) {
      console.error('Invalid date:', { workDate, timeSpan, utcDateTimeStr });
      return null;
    }

    // 3. 转换为北京时间（+8小时）
    const beijingDateTime = new Date(utcDateTime.getTime() + 8 * 60 * 60 * 1000);

    // 4. 格式化输出（使用 UTC 方法获取已调整的时间）
    const year = beijingDateTime.getUTCFullYear();
    const month = String(beijingDateTime.getUTCMonth() + 1).padStart(2, '0');
    const day = String(beijingDateTime.getUTCDate()).padStart(2, '0');
    const hour = String(beijingDateTime.getUTCHours()).padStart(2, '0');
    const minute = String(beijingDateTime.getUTCMinutes()).padStart(2, '0');

    return {
      date: `${year}-${month}-${day}`,
      time: `${hour}:${minute}`,
      dateTime: beijingDateTime,
      display: `${year}-${month}-${day} ${hour}:${minute}`
    };
  } catch (error) {
    console.error('时间转换错误:', error, { workDate, timeSpan });
    return null;
  }
}

/**
 * 处理排班时间范围（包括跨天班次）
 * 
 * @param {string} workDate - UTC工作日期 "2025-12-28"
 * @param {string} startTime - 开始时间 "22:00"
 * @param {string} endTime - 结束时间 "06:00"
 * @returns {Object} 转换后的时间范围对象
 * @returns {Object} returns.start - 开始时间对象（同 convertUTCScheduleToBeijing 返回）
 * @returns {Object} returns.end - 结束时间对象
 * @returns {boolean} returns.isCrossingDay - 是否跨天班次
 * 
 * @example
 * // 夜班：UTC 2025-12-28 22:00 - 2025-12-29 06:00
 * // → 北京 2025-12-29 06:00 - 2025-12-29 14:00
 * convertScheduleTimeRange('2025-12-28', '22:00', '06:00')
 * // => { start: {...}, end: {...}, isCrossingDay: true }
 */
export function convertScheduleTimeRange(workDate, startTime, endTime) {
  if (!workDate || !startTime || !endTime) {
    return null;
  }

  try {
    // 转换开始时间
    const start = convertUTCScheduleToBeijing(workDate, startTime);
    if (!start) return null;

    // 判断是否跨天：StartTime 的小时数 > EndTime 的小时数
    const [startH] = startTime.split(':').map(Number);
    const [endH] = endTime.split(':').map(Number);
    const isCrossingDay = startH > endH;

    // 计算结束日期
    let endDate = workDate;
    if (isCrossingDay) {
      // 跨天班次：结束日期为第二天
      const date = new Date(workDate + 'T00:00:00Z');
      date.setUTCDate(date.getUTCDate() + 1);
      endDate = date.toISOString().split('T')[0];
    }

    // 转换结束时间
    const end = convertUTCScheduleToBeijing(endDate, endTime);
    if (!end) return null;

    return {
      start,
      end,
      isCrossingDay,
      // 额外提供便捷的显示格式
      displayRange: isCrossingDay 
        ? `${start.display} - 次日 ${end.time}`
        : `${start.time} - ${end.time}`
    };
  } catch (error) {
    console.error('时间范围转换错误:', error, { workDate, startTime, endTime });
    return null;
  }
}

/**
 * 将北京时间日期范围转换为 UTC 日期范围（用于API查询）
 * 
 * 注意：由于时区偏移，需要扩大查询范围！
 * 北京时间 2025-01-01 00:00 = UTC 2024-12-31 16:00
 * 北京时间 2025-01-07 23:59 = UTC 2025-01-07 15:59
 * 
 * @param {string} beijingStartDate - 北京时间开始日期 "2025-01-01"
 * @param {string} beijingEndDate - 北京时间结束日期 "2025-01-07"
 * @returns {Object} UTC日期范围
 * @returns {string} returns.utcStartDate - UTC开始日期（可能是前一天）
 * @returns {string} returns.utcEndDate - UTC结束日期
 * 
 * @example
 * // 查询北京时间 2025-01-01 ~ 2025-01-07 的排班
 * convertBeijingDateRangeToUTC('2025-01-01', '2025-01-07')
 * // => { utcStartDate: '2024-12-31', utcEndDate: '2025-01-07' }
 */
export function convertBeijingDateRangeToUTC(beijingStartDate, beijingEndDate) {
  if (!beijingStartDate || !beijingEndDate) {
    return null;
  }

  try {
    // 北京时间的开始时刻（00:00）转 UTC
    // 使用 +08:00 时区标识明确指定为北京时间
    const startUTC = new Date(`${beijingStartDate}T00:00:00+08:00`);
    const startDate = startUTC.toISOString().split('T')[0];

    // 北京时间的结束时刻（23:59:59）转 UTC
    const endUTC = new Date(`${beijingEndDate}T23:59:59+08:00`);
    const endDate = endUTC.toISOString().split('T')[0];

    return {
      utcStartDate: startDate,
      utcEndDate: endDate
    };
  } catch (error) {
    console.error('日期范围转换错误:', error, { beijingStartDate, beijingEndDate });
    return null;
  }
}

/**
 * 批量处理排班数据的时区转换
 * 
 * @param {Array} schedules - 后端返回的排班数据数组
 * @returns {Array} 转换后的排班数据数组
 * 
 * @example
 * const schedules = [
 *   { workDate: '2025-12-28', startTime: '08:00', endTime: '16:00', ... }
 * ];
 * const processed = processScheduleData(schedules);
 * // 每个对象新增字段：beijingStartDate, beijingStartTime, beijingEndDate, beijingEndTime, etc.
 */
export function processScheduleData(schedules) {
  if (!Array.isArray(schedules)) {
    return [];
  }

  return schedules.map(schedule => {
    const timeRange = convertScheduleTimeRange(
      schedule.workDate,
      schedule.startTime,
      schedule.endTime
    );

    if (!timeRange) {
      // 转换失败，保留原始数据
      console.warn('排班数据转换失败:', schedule);
      return schedule;
    }

    return {
      ...schedule,
      // 北京时间字段
      beijingStartDate: timeRange.start.date,
      beijingStartTime: timeRange.start.time,
      beijingEndDate: timeRange.end.date,
      beijingEndTime: timeRange.end.time,
      beijingStartDateTime: timeRange.start.dateTime,
      beijingEndDateTime: timeRange.end.dateTime,
      // 显示格式
      displayStart: timeRange.start.display,
      displayEnd: timeRange.end.display,
      displayRange: timeRange.displayRange,
      // 是否跨天
      isCrossingDay: timeRange.isCrossingDay
    };
  });
}

/**
 * 获取当前周的日期范围（北京时间）
 * 
 * @param {Date} currentDate - 当前日期
 * @returns {Object} 周日期范围
 * @returns {string} returns.startDate - 周一日期
 * @returns {string} returns.endDate - 周日日期
 * @returns {Array} returns.weekDays - 7天的日期数组
 */
export function getWeekRange(currentDate = new Date()) {
  const date = new Date(currentDate);
  const dayOfWeek = date.getDay();
  
  // 调整到周一（周日为0，需要特殊处理）
  const startOfWeek = new Date(date);
  const daysToMonday = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;
  startOfWeek.setDate(date.getDate() + daysToMonday);

  const weekDays = [];
  for (let i = 0; i < 7; i++) {
    const day = new Date(startOfWeek);
    day.setDate(startOfWeek.getDate() + i);
    weekDays.push(day.toISOString().split('T')[0]);
  }

  return {
    startDate: weekDays[0],
    endDate: weekDays[6],
    weekDays
  };
}

/**
 * 获取当前月的日期范围（北京时间）
 * 
 * @param {Date} currentDate - 当前日期
 * @returns {Object} 月日期范围
 * @returns {string} returns.startDate - 月初日期
 * @returns {string} returns.endDate - 月末日期
 * @returns {number} returns.year - 年份
 * @returns {number} returns.month - 月份（1-12）
 */
export function getMonthRange(currentDate = new Date()) {
  const date = new Date(currentDate);
  const year = date.getFullYear();
  const month = date.getMonth();

  const firstDay = new Date(year, month, 1);
  const lastDay = new Date(year, month + 1, 0);

  return {
    startDate: firstDay.toISOString().split('T')[0],
    endDate: lastDay.toISOString().split('T')[0],
    year,
    month: month + 1
  };
}
