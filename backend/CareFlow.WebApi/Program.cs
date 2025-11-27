using CareFlow.Infrastructure; // 引用基础设施层

var builder = WebApplication.CreateBuilder(args);

// ==============================================
// 1. 配置服务 (Services) - 依赖注入的注册
// ==============================================

// 添加控制器
builder.Services.AddControllers();

// 添加 Swagger/OpenAPI (接口文档生成器)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// [关键] 注册基础设施层 (数据库、Repository等都在这里面)
// 这行代码会调用我们在 Infrastructure 层写的 DependencyInjection 类
builder.Services.AddInfrastructure(builder.Configuration);

// [关键] 配置 CORS (跨域资源共享)
// 允许你的前端 (Vue) 访问这个后端
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()   // 允许任何来源 (开发阶段为了方便)
                  .AllowAnyMethod()   // 允许任何 HTTP 方法 (GET, POST...)
                  .AllowAnyHeader();  // 允许任何 Header
        });
});

var app = builder.Build();

// ==============================================
// 2. 配置 HTTP 请求管道 (Pipeline)
// ==============================================

// 如果是开发环境，启用 Swagger 页面
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 启用 HTTPS 重定向
app.UseHttpsRedirection();

// 启用 CORS (必须放在 UseAuthorization 之前)
app.UseCors("AllowAll");

// 启用权限验证 (虽然还没写逻辑，先放着)
app.UseAuthorization();

// 自动匹配控制器路由
app.MapControllers();

// 启动程序！
app.Run();