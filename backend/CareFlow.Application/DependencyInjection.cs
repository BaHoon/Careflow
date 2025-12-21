using CareFlow.Application.Interfaces;
using CareFlow.Application.Services;
using CareFlow.Application.Services.InspectionOrders;
using CareFlow.Application.Services.MedicationOrders;
using CareFlow.Application.Services.OperationOrders;
using CareFlow.Application.Services.OrderAcknowledgement;
using CareFlow.Application.Services.SurgicalOrders;
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
        
        // 注册任务工厂
        services.AddScoped<IExecutionTaskFactory, SurgicalExecutionTaskFactory>();

        // 注册医嘱服务（分离接口方案）
        services.AddScoped<IMedicationOrderService, MedicationOrderService>();
        services.AddScoped<IInspectionOrderService, InspectionOrderService>();
        services.AddScoped<ISurgicalOrderService, SurgicalOrderService>();
        services.AddScoped<IOperationOrderService, OperationOrderService>();
        
        // 注册医嘱签收服务（阶段一）
        services.AddScoped<IOrderAcknowledgementService, OrderAcknowledgementService>();

        return services;
    }
}