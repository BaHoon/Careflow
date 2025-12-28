-- ============================================================
-- 患者日志功能 - 数据库索引优化脚本
-- 目的：优化患者日志查询性能
-- 数据库：PostgreSQL
-- 创建日期：2025-12-28
-- ============================================================

-- ============================
-- 1. ExecutionTasks 表索引
-- ============================

-- 索引：患者ID + 实际执行开始时间 (用于查询某患者在特定时间段内的医嘱执行记录)
-- 条件索引：只为已执行的任务创建索引 (ActualStartTime IS NOT NULL)
CREATE INDEX IF NOT EXISTS "IX_ExecutionTasks_PatientId_ActualStartTime"
    ON "ExecutionTasks" ("PatientId", "ActualStartTime")
    WHERE "ActualStartTime" IS NOT NULL;

COMMENT ON INDEX "IX_ExecutionTasks_PatientId_ActualStartTime" IS 
'患者日志查询优化：用于快速检索患者的医嘱执行记录(按实际执行时间筛选)';

-- ============================
-- 2. VitalSignsRecords 表索引
-- ============================

-- 索引：患者ID + 记录时间 (用于查询某患者在特定时间段内的护理记录)
CREATE INDEX IF NOT EXISTS "IX_VitalSignsRecords_PatientId_RecordTime"
    ON "VitalSignsRecords" ("PatientId", "RecordTime");

COMMENT ON INDEX "IX_VitalSignsRecords_PatientId_RecordTime" IS 
'患者日志查询优化：用于快速检索患者的护理记录(按记录时间筛选)';

-- ============================
-- 3. InspectionReports 表索引
-- ============================

-- 索引：患者ID + 报告时间 (用于查询某患者在特定时间段内的检查报告)
CREATE INDEX IF NOT EXISTS "IX_InspectionReports_PatientId_ReportTime"
    ON "InspectionReports" ("PatientId", "ReportTime");

COMMENT ON INDEX "IX_InspectionReports_PatientId_ReportTime" IS 
'患者日志查询优化：用于快速检索患者的检查报告(按报告时间筛选)';

-- ============================
-- 4. 验证索引创建结果
-- ============================

-- 查询新创建的索引信息
SELECT 
    schemaname AS "Schema",
    tablename AS "Table",
    indexname AS "Index Name",
    indexdef AS "Index Definition"
FROM 
    pg_indexes
WHERE 
    indexname IN (
        'IX_ExecutionTasks_PatientId_ActualStartTime',
        'IX_VitalSignsRecords_PatientId_RecordTime',
        'IX_InspectionReports_PatientId_ReportTime'
    )
ORDER BY 
    tablename, indexname;

-- ============================
-- 5. 性能分析建议
-- ============================

-- 使用 EXPLAIN ANALYZE 分析查询计划 (示例)
-- 执行前：
-- EXPLAIN ANALYZE
-- SELECT * FROM "ExecutionTasks"
-- WHERE "PatientId" = 'P001'
--   AND "ActualStartTime" >= '2025-12-26 00:00:00'
--   AND "ActualStartTime" <= '2025-12-28 23:59:59'
--   AND "ActualStartTime" IS NOT NULL;

-- 预期：索引创建后，查询计划应显示使用 Index Scan，而非 Seq Scan

-- ============================
-- 6. 索引维护建议
-- ============================

-- 定期更新统计信息以优化查询计划
-- ANALYZE "ExecutionTasks";
-- ANALYZE "VitalSignsRecords";
-- ANALYZE "InspectionReports";

-- 定期重建索引以清理碎片 (在维护窗口执行)
-- REINDEX INDEX CONCURRENTLY "IX_ExecutionTasks_PatientId_ActualStartTime";
-- REINDEX INDEX CONCURRENTLY "IX_VitalSignsRecords_PatientId_RecordTime";
-- REINDEX INDEX CONCURRENTLY "IX_InspectionReports_PatientId_ReportTime";

-- ============================
-- 7. 回滚脚本 (如需删除索引)
-- ============================

-- DROP INDEX IF EXISTS "IX_ExecutionTasks_PatientId_ActualStartTime";
-- DROP INDEX IF EXISTS "IX_VitalSignsRecords_PatientId_RecordTime";
-- DROP INDEX IF EXISTS "IX_InspectionReports_PatientId_ReportTime";
