/**
 * 时区转换工具
 * 统一处理前后端时间交互，确保使用北京时间（UTC+8）
 */

/**
 * 将前端本地时间字符串转换为 ISO 8601 格式（带时区）
 * @param {string} localTimeString - 格式: "2025-12-19T08:00:00" (无时区信息)
 * @returns {string} - 格式: "2025-12-19T08:00:00+08:00" (带北京时区)
 */
export function toBeijingTimeISO(localTimeString) {
  if (!localTimeString) return null;
  
  // 如果已经包含时区信息，直接返回
  if (localTimeString.includes('+') || localTimeString.includes('Z')) {
    return localTimeString;
  }
  
  // 添加北京时区标识 (+08:00)
  return localTimeString + '+08:00';
}

/**
 * 将日期对象转换为 ISO 8601 格式（带北京时区）
 * @param {Date} date - JavaScript Date 对象
 * @returns {string} - 格式: "2025-12-19T08:00:00+08:00"
 */
export function dateToBeijingISO(date) {
  if (!date) return null;
  
  // 获取本地时间字符串（假设用户在东八区）
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  const seconds = String(date.getSeconds()).padStart(2, '0');
  
  return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}+08:00`;
}

/**
 * 将后端返回的UTC时间转换为北京时间显示
 * @param {string} utcTimeString - UTC时间字符串
 * @returns {string} - 北京时间字符串，格式: "2025-12-19 08:00"
 */
export function utcToBeijingDisplay(utcTimeString) {
  if (!utcTimeString) return '';
  
  const date = new Date(utcTimeString);
  
  // 转换为北京时间（UTC+8）
  const beijingDate = new Date(date.getTime() + 8 * 60 * 60 * 1000);
  
  const year = beijingDate.getUTCFullYear();
  const month = String(beijingDate.getUTCMonth() + 1).padStart(2, '0');
  const day = String(beijingDate.getUTCDate()).padStart(2, '0');
  const hours = String(beijingDate.getUTCHours()).padStart(2, '0');
  const minutes = String(beijingDate.getUTCMinutes()).padStart(2, '0');
  
  return `${year}-${month}-${day} ${hours}:${minutes}`;
}

/**
 * 转换医嘱对象中的所有时间字段为北京时间ISO格式
 * @param {Object} order - 医嘱对象
 * @returns {Object} - 转换后的医嘱对象
 */
export function convertOrderTimesToBeijing(order) {
  return {
    ...order,
    startTime: toBeijingTimeISO(order.startTime),
    plantEndTime: toBeijingTimeISO(order.plantEndTime)
  };
}
