using System;
using System.Linq;

namespace CareFlow.Core.Utils
{
    /// <summary>
    /// 时区工具类
    /// 统一处理中国时间（UTC+8）
    /// 重要：整个系统全部使用中国时间，数据库存储也使用中国时间
    /// </summary>
    public static class TimeZoneHelper
    {
        /// <summary>
        /// 中国时区（UTC+8）
        /// </summary>
        private static readonly TimeZoneInfo ChinaTimeZone = GetChinaTimeZone();
        
        /// <summary>
        /// 时区偏移量（8小时）
        /// </summary>
        private static readonly TimeSpan ChinaTimeOffset = TimeSpan.FromHours(8);

        /// <summary>
        /// 获取中国时区，支持Windows和Linux系统
        /// </summary>
        private static TimeZoneInfo GetChinaTimeZone()
        {
            // Windows系统使用 "China Standard Time"
            // Linux/Mac系统使用 "Asia/Shanghai"
            try
            {
                if (TimeZoneInfo.GetSystemTimeZones().Any(tz => tz.Id == "China Standard Time"))
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                }
            }
            catch
            {
                // 如果找不到，继续尝试其他时区ID
            }

            try
            {
                if (TimeZoneInfo.GetSystemTimeZones().Any(tz => tz.Id == "Asia/Shanghai"))
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
                }
            }
            catch
            {
                // 如果找不到，使用UTC+8的固定偏移量
            }

            // 如果都找不到，创建一个UTC+8的固定时区
            return TimeZoneInfo.CreateCustomTimeZone("China Standard Time", TimeSpan.FromHours(8), "China Standard Time", "China Standard Time");
        }

        /// <summary>
        /// 获取当前中国时间（北京时间）
        /// 替代 DateTime.UtcNow，系统统一使用此方法获取当前时间
        /// </summary>
        public static DateTime GetChinaTimeNow()
        {
            var utcNow = DateTime.UtcNow;
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, ChinaTimeZone);
        }

        /// <summary>
        /// 将UTC时间（或系统时间）转换为中国时间
        /// 用于：从数据库读取时间后，如果数据库存储的是UTC，需要转换为中国时间
        /// 注意：如果数据库已经存储中国时间，则不需要转换，直接使用
        /// </summary>
        public static DateTime ToChinaTime(DateTime time)
        {
            // 如果时间已经是中国时间（Kind为Unspecified且我们假设数据库存储的是中国时间），直接返回
            if (time.Kind == DateTimeKind.Unspecified)
            {
                // 数据库存储的时间，假设为中国时间，直接返回
                return time;
            }
            
            // 如果是UTC时间，转换为中国时间
            if (time.Kind == DateTimeKind.Utc)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(time, ChinaTimeZone);
            }
            
            // 如果是本地时间，先转换为UTC再转换为中国时间
            if (time.Kind == DateTimeKind.Local)
            {
                var utcTime = time.ToUniversalTime();
                return TimeZoneInfo.ConvertTimeFromUtc(utcTime, ChinaTimeZone);
            }
            
            // Unspecified 类型，假设为中国时间
            return time;
        }

        /// <summary>
        /// 存储时间到数据库（直接存储中国时间，不转换）
        /// 重要：数据库存储使用中国时间，不需要转换为UTC
        /// </summary>
        public static DateTime StoreChinaTime(DateTime chinaTime)
        {
            // 直接返回中国时间，数据库存储时使用 Unspecified Kind
            if (chinaTime.Kind == DateTimeKind.Utc)
            {
                // 如果输入是UTC，先转换为中国时间
                return TimeZoneInfo.ConvertTimeFromUtc(chinaTime, ChinaTimeZone);
            }
            
            // 返回时间，Kind 为 Unspecified（数据库存储时EF Core会处理）
            return DateTime.SpecifyKind(chinaTime, DateTimeKind.Unspecified);
        }

        /// <summary>
        /// 将UTC时间转换为中国时间（兼容旧代码，但建议使用 ToChinaTime）
        /// </summary>
        [Obsolete("建议使用 ToChinaTime 方法，此方法保留用于兼容")]
        public static DateTime ToUtcTime(DateTime chinaTime)
        {
            // 此方法已废弃，因为数据库现在存储中国时间
            // 保留用于兼容，但实际应该直接存储中国时间
            return StoreChinaTime(chinaTime);
        }

        /// <summary>
        /// 获取中国时间的日期部分（不包含时间）
        /// </summary>
        public static DateTime GetChinaTimeToday()
        {
            return GetChinaTimeNow().Date;
        }

        /// <summary>
        /// 获取中国时间的日期部分（不包含时间）
        /// </summary>
        public static DateTime GetChinaTimeDate(DateTime dateTime)
        {
            // 数据库存储的是中国时间，直接使用
            return dateTime.Date;
        }
        
        /// <summary>
        /// 比较两个时间（确保都是中国时间）
        /// 用于时间比较，确保比较时使用相同的时区
        /// </summary>
        public static int CompareChinaTime(DateTime time1, DateTime time2)
        {
            var chinaTime1 = ToChinaTime(time1);
            var chinaTime2 = ToChinaTime(time2);
            return DateTime.Compare(chinaTime1, chinaTime2);
        }
        
        /// <summary>
        /// 检查时间是否已过（基于当前中国时间）
        /// </summary>
        public static bool IsTimePassed(DateTime time)
        {
            var chinaTime = ToChinaTime(time);
            var now = GetChinaTimeNow();
            return chinaTime < now;
        }
    }
}

