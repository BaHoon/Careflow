using CareFlow.Application.Interfaces;
using CareFlow.Application.Services;
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
        services.AddScoped<IMedicationOrderTaskService, MedicationOrderTaskService>();

        return services;
    }
}