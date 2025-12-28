namespace CareFlow.Core.Enums
{
    /// <summary>
    /// 系统操作类型枚举
    /// </summary>
    public enum SystemOperationType
    {
        /// <summary>
        /// 登录
        /// </summary>
        Login,

        /// <summary>
        /// 登出
        /// </summary>
        Logout,

        /// <summary>
        /// 医嘱停止
        /// </summary>
        OrderStop,

        /// <summary>
        /// 药品核对失败
        /// </summary>
        DrugVerificationFailed,

        /// <summary>
        /// 账号创建
        /// </summary>
        AccountCreated,

        /// <summary>
        /// 账号修改
        /// </summary>
        AccountModified,

        /// <summary>
        /// 账号删除
        /// </summary>
        AccountDeleted,

        /// <summary>
        /// 密码修改
        /// </summary>
        PasswordChanged,

        /// <summary>
        /// 权限修改
        /// </summary>
        PermissionChanged,

        /// <summary>
        /// 科室创建
        /// </summary>
        DepartmentCreated,

        /// <summary>
        /// 科室修改
        /// </summary>
        DepartmentModified,

        /// <summary>
        /// 科室删除
        /// </summary>
        DepartmentDeleted,

        /// <summary>
        /// 医嘱创建
        /// </summary>
        OrderCreated,

        /// <summary>
        /// 医嘱修改
        /// </summary>
        OrderModified,

        /// <summary>
        /// 任务完成
        /// </summary>
        TaskCompleted,

        /// <summary>
        /// 数据导入
        /// </summary>
        DataImport,

        /// <summary>
        /// 数据导出
        /// </summary>
        DataExport,

        /// <summary>
        /// 其他操作
        /// </summary>
        Other
    }
}
