using CareFlow.Application.Interfaces;
using CareFlow.Application.Services;
using CareFlow.Application.Services.DischargeOrders;
using CareFlow.Application.Services.InspectionOrders;
using CareFlow.Application.Services.MedicalOrder;
using CareFlow.Application.Services.MedicationOrders;
using CareFlow.Application.Services.OperationOrders;
using CareFlow.Application.Services.OrderAcknowledgement;
using CareFlow.Application.Services.OrderApplication;
using CareFlow.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CareFlow.Application;

public static class DependencyInjection
{
    /// <summary>
    /// 添加应用层服务
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 注册应用服务
        services.AddScoped<IBarcodeMatchingService, BarcodeMatchingService>();
        
        // 注册任务生成服务
        services.AddScoped<IMedicationOrderTaskService, MedicationOrderTaskService>();
        services.AddScoped<ISurgicalOrderTaskService, SurgicalOrderTaskService>();
        services.AddScoped<IDischargeOrderTaskService, DischargeOrderTaskService>();
        services.AddScoped<IOperationOrderTaskService, OperationOrderTaskService>();
        
        // 注册任务工厂
        services.AddScoped<IExecutionTaskFactory, SurgicalExecutionTaskFactory>();

        // 注册医嘱服务（分离接口方案）
        services.AddScoped<IMedicationOrderService, MedicationOrderService>();
        services.AddScoped<IInspectionOrderService, InspectionOrderService>();
        services.AddScoped<ISurgicalOrderService, SurgicalOrderService>();
        services.AddScoped<IOperationOrderService, OperationOrderService>();
        services.AddScoped<IDischargeOrderService, DischargeOrderService>();
        
        // 注册操作医嘱管理服务（兼容旧接口，OperationOrderService实现了IOperationOrderManager）
        services.AddScoped<IOperationOrderManager, OperationOrderService>();
        
        // 注册医嘱签收服务（阶段一）
        services.AddScoped<IOrderAcknowledgementService, OrderAcknowledgementService>();
        
        // 注册医嘱申请服务（阶段二）
        services.AddScoped<IOrderApplicationService, OrderApplicationService>();
        services.AddScoped<IPharmacyIntegrationService, PharmacyIntegrationService>();
        services.AddScoped<IInspectionStationService, InspectionStationService>();
        services.AddScoped<IBackgroundJobService, SimpleBackgroundJobService>();
        
        // 注册医生端医嘱查询服务
        services.AddScoped<IMedicalOrderQueryService, MedicalOrderQueryService>();

        return services;
    }
}