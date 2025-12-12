using CareFlow.Core.Interfaces;
using CareFlow.Infrastructure.Services;
using CareFlow.Infrastructure; // 引用基础设施层
using CareFlow.Application; // 引用应用层
using CareFlow.Application.Services; // 引用应用层服务
using CareFlow.Infrastructure; // 添加这个using来引用ApplicationDbContext
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==============================================
// 1. 配置服务 (Services) - 依赖注入的注册
// ==============================================

// 添加控制器
builder.Services.AddControllers();

// 添加 Swagger/OpenAPI (接口文档生成器)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册 AuthService
builder.Services.AddScoped<AuthService>();

// 注册手术医嘱任务服务及工厂
builder.Services.AddScoped<IExecutionTaskFactory, SurgicalExecutionTaskFactory>();
builder.Services.AddScoped<ISurgicalOrderTaskService, SurgicalOrderTaskService>();

// 注册检查类医嘱服务
builder.Services.AddScoped<CareFlow.Application.Interfaces.IInspectionService, CareFlow.Application.Services.InspectionService>();

// // 注册药品医嘱任务服务
builder.Services.AddScoped<IMedicationOrderTaskService, MedicationOrderTaskService>();

// 注册护士分配计算服务
builder.Services.AddScoped<CareFlow.Application.Interfaces.INurseAssignmentService, CareFlow.Application.Services.NurseAssignmentService>();

// 注册医嘱管理服务
builder.Services.AddScoped<CareFlow.Application.Services.IMedicalOrderManager, CareFlow.Application.Services.MedicalOrderManager>();

// 配置 JWT 认证服务
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// [关键] 注册基础设施层 (数据库、Repository等都在这里面)
// 这行代码会调用我们在 Infrastructure 层写的 DependencyInjection 类
builder.Services.AddInfrastructure(builder.Configuration);

// [关键] 注册应用层服务
builder.Services.AddApplication();

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
// 数据库初始化与数据预置 (Data Seeding)
// ==============================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // 1. 获取数据库上下文服务
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // 2. 调用我们在 Infrastructure 层写的初始化器
        // 确保你的 DbInitializer.cs 类名和命名空间正确
        CareFlow.Infrastructure.Data.DbInitializer.Initialize(context); // 1) 播种所有虚拟基础数据

        var taskFactory = services.GetRequiredService<IExecutionTaskFactory>();
        // var createdTasks = EnsureSurgicalExecutionTasks(context, taskFactory); // 2) 手术医嘱 -> 术前任务拆分
        
        // 这是一个可选的日志输出，方便你看控制台知道发生了什么
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("数据库初始化已完成，测试数据已插入。");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "数据库初始化期间发生错误。");
    }
}

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

// 启用认证 (必须在 UseAuthorization 之前)
app.UseAuthentication(); 
app.UseAuthorization();

// 启用权限验证 (虽然还没写逻辑，先放着)
app.UseAuthorization();

// 自动匹配控制器路由
app.MapControllers();

// 启动程序！
app.Run();
