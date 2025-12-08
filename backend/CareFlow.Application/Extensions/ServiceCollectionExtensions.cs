using CareFlow.Application.Services;
using CareFlow.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CareFlow.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 注册应用层服务
        services.AddScoped<AuthService>();
        services.AddScoped<IMedicationOrderTaskService, MedicationOrderTaskService>();
        
        return services;
    }
}