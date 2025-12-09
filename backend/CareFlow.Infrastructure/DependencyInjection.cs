using CareFlow.Core.Interfaces;
using CareFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CareFlow.Infrastructure.Repositories;

namespace CareFlow.Infrastructure;

public static class DependencyInjection
{
    // 这是一个扩展方法
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. 注入数据库上下文 (连接字符串从 WebApi 的配置文件里读)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString)); // 使用 PostgreSQL

        // 2. 注入仓储 (Repository)
        // 这里的写法意思是：只要有人要 IRepository<User, Guid>，我就给他 EfRepository<User, Guid>
        // 这是一个比较高级的通用注入写法，或者你可以写死：
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));

        // 注册条形码服务
        services.AddScoped<IBarcodeService, AsposeBarcodeService>();
        
        // 注册记录验证服务
        services.AddScoped<IRecordValidationService, RecordValidationService>();

        // 注册护士排班专用仓储
        services.AddScoped<INurseScheduleRepository, NurseScheduleRepository>();

        return services;
    }
}