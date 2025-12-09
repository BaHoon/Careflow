-- 创建 BarcodeIndexes 表的 SQL 脚本
CREATE TABLE IF NOT EXISTS "BarcodeIndexes" (
    "Id" text NOT NULL,
    "TableName" text NOT NULL,
    "RecordId" text NOT NULL,
    "CreateTime" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_BarcodeIndexes" PRIMARY KEY ("Id")
);