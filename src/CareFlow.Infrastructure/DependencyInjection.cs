using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CareFlow.Core.Interfaces;  // 导入 IRepository 接口所在的命名空间




namespace CareFlow.Infrastructure
{
    public static class DependencyInjection
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // 配置 DbContext 使用 PostgreSQL 连接字符串
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // 注册泛型仓库
            services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

            // 其他基础设施服务（例如权限验证器）可以在这里注册
        }
    }
}
