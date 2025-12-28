using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarcodePrintController : ControllerBase
{
    private readonly IRepository<ExecutionTask, long> _taskRepository;
    private readonly ILogger<BarcodePrintController> _logger;
    private readonly IWebHostEnvironment _environment;

    public BarcodePrintController(
        IRepository<ExecutionTask, long> taskRepository,
        ILogger<BarcodePrintController> logger,
        IWebHostEnvironment environment)
    {
        _taskRepository = taskRepository;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// 获取所有生成的任务条形码列表
    /// </summary>
    [HttpGet("task-barcodes")]
    public async Task<IActionResult> GetTaskBarcodes([FromQuery] string? wardId = null)
    {
        try
        {
            _logger.LogInformation("🔍 开始获取任务条形码列表，wardId: {WardId}", wardId ?? "全部");
            
            // 获取条形码文件目录
            var barcodeDir = Path.Combine(_environment.WebRootPath, "barcodes", "ExecutionTasks");
            
            _logger.LogInformation("📁 条形码目录: {Dir}", barcodeDir);
            
            if (!Directory.Exists(barcodeDir))
            {
                _logger.LogWarning("⚠️ 条形码目录不存在: {Dir}", barcodeDir);
                return Ok(new
                {
                    success = true,
                    count = 0,
                    data = new List<object>(),
                    message = "条形码目录不存在，可能还没有生成任何任务条形码"
                });
            }

            // 递归获取所有PNG文件
            var barcodeFiles = Directory.GetFiles(barcodeDir, "*.png", SearchOption.AllDirectories)
                .OrderByDescending(f => new FileInfo(f).CreationTime)
                .Take(100) // 限制最多返回100条
                .ToList();

            _logger.LogInformation("📄 找到 {Count} 个PNG文件", barcodeFiles.Count);
            
            var result = new List<object>();

            foreach (var filePath in barcodeFiles)
            {
                try
                {
                    // 从文件名中提取任务ID：ExecutionTasks-{taskId}.png
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var parts = fileName.Split('-');
                    
                    _logger.LogDebug("处理文件: {FileName}", fileName);
                    
                    if (parts.Length >= 2 && long.TryParse(parts[1], out long taskId))
                    {
                        // 查询任务信息
                        var task = await _taskRepository.GetQueryable()
                            .Include(t => t.Patient)
                                .ThenInclude(p => p.Bed)
                                    .ThenInclude(b => b.Ward)
                            .Include(t => t.MedicalOrder)
                            .FirstOrDefaultAsync(t => t.Id == taskId);

                        if (task == null)
                        {
                            _logger.LogWarning("⚠️ 任务 {TaskId} 不存在，跳过", taskId);
                            continue;
                        }

                        // 如果指定了病区，则筛选
                        if (!string.IsNullOrEmpty(wardId) && task.Patient?.Bed?.Ward?.Id != wardId)
                        {
                            _logger.LogDebug("任务 {TaskId} 不在指定病区 {WardId}，跳过", taskId, wardId);
                            continue;
                        }

                        // 构建相对URL路径
                        var relativePath = filePath.Replace(_environment.WebRootPath, "").Replace("\\", "/");
                        
                        // 读取图片并转换为base64
                        byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                        string base64Image = Convert.ToBase64String(imageBytes);
                        string base64DataUrl = $"data:image/png;base64,{base64Image}";
                        
                        result.Add(new
                        {
                            taskId = task.Id,
                            patientId = task.PatientId,
                            patientName = task.Patient?.Name,
                            orderId = task.MedicalOrderId,
                            orderSummary = GetOrderSummary(task),
                            taskCategory = task.Category.ToString(),
                            plannedTime = task.PlannedStartTime,
                            barcodeUrl = relativePath,
                            barcodeBase64 = base64DataUrl, // 直接返回base64数据
                            generatedTime = new FileInfo(filePath).CreationTime
                        });
                        
                        _logger.LogDebug("✅ 添加任务 {TaskId} 的条形码到结果列表", taskId);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ 无法从文件名 {FileName} 解析任务ID", fileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "处理条形码文件 {FilePath} 失败", filePath);
                    continue;
                }
            }

            _logger.LogInformation("✅ 成功加载 {Count} 个任务条形码", result.Count);
            
            return Ok(new
            {
                success = true,
                count = result.Count,
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取任务条形码列表失败");
            return StatusCode(500, new { success = false, message = $"获取失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 获取指定任务的条形码图片
    /// </summary>
    [HttpGet("task-barcodes/{taskId}/image")]
    public async Task<IActionResult> GetTaskBarcodeImage(long taskId)
    {
        try
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                return NotFound(new { success = false, message = "任务不存在" });
            }

            // 查找条形码文件
            var barcodeDir = Path.Combine(_environment.WebRootPath, "barcodes", "ExecutionTasks");
            var barcodeFiles = Directory.GetFiles(barcodeDir, $"ExecutionTasks-{taskId}.png", SearchOption.AllDirectories);
            
            if (barcodeFiles.Length == 0)
            {
                return NotFound(new { success = false, message = "任务条形码未生成" });
            }

            var filePath = barcodeFiles[0];
            
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { success = false, message = "条形码文件不存在" });
            }

            var imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(imageBytes, "image/png", $"Task-{taskId}-barcode.png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取任务 {TaskId} 的条形码图片失败", taskId);
            return StatusCode(500, new { success = false, message = $"获取失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 生成或获取指定任务的条形码（返回base64编码）
    /// </summary>
    [HttpGet("generate-task-barcode")]
    public async Task<IActionResult> GenerateTaskBarcode([FromQuery] long taskId)
    {
        try
        {
            // 查询任务信息（包含关联数据）
            var task = await _taskRepository.GetQueryable()
                .Include(t => t.Patient)
                    .ThenInclude(p => p.Bed)
                        .ThenInclude(b => b.Ward)
                .Include(t => t.MedicalOrder)
                .FirstOrDefaultAsync(t => t.Id == taskId);
                
            if (task == null)
            {
                return NotFound(new { success = false, message = "任务不存在" });
            }

            // 查找条形码文件
            var barcodeDir = Path.Combine(_environment.WebRootPath, "barcodes", "ExecutionTasks");
            if (!Directory.Exists(barcodeDir))
            {
                Directory.CreateDirectory(barcodeDir);
            }

            var barcodeFiles = Directory.GetFiles(barcodeDir, $"ExecutionTasks-{taskId}.png", SearchOption.AllDirectories);
            
            string filePath;
            if (barcodeFiles.Length == 0)
            {
                // 条形码未生成，返回提示（实际生成逻辑应该在任务创建时完成）
                return NotFound(new { 
                    success = false, 
                    message = "任务条形码未生成，请先在医嘱签收时生成条形码" 
                });
            }
            else
            {
                filePath = barcodeFiles[0];
            }
            
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { success = false, message = "条形码文件不存在" });
            }

            // 读取图片并转换为base64
            byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            string base64Image = Convert.ToBase64String(imageBytes);
            string base64DataUrl = $"data:image/png;base64,{base64Image}";
            
            // 构建相对URL路径
            var relativePath = filePath.Replace(_environment.WebRootPath, "").Replace("\\", "/");

            return Ok(new
            {
                success = true,
                data = new
                {
                    taskId = task.Id,
                    patientId = task.PatientId,
                    patientName = task.Patient?.Name,
                    orderId = task.MedicalOrderId,
                    orderSummary = GetOrderSummary(task),
                    taskCategory = task.Category.ToString(),
                    plannedTime = task.PlannedStartTime,
                    barcodeBase64 = base64DataUrl,
                    barcodeUrl = relativePath,
                    generatedTime = new FileInfo(filePath).CreationTime
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取任务 {TaskId} 的条形码失败", taskId);
            return StatusCode(500, new { success = false, message = $"获取失败: {ex.Message}" });
        }
    }

    private string GetOrderSummary(ExecutionTask task)
    {
        if (task.MedicalOrder == null)
        {
            return "未知医嘱";
        }

        // 从 DataPayload 中提取任务描述
        try
        {
            if (!string.IsNullOrEmpty(task.DataPayload))
            {
                _logger.LogDebug("任务 {TaskId} 的 DataPayload: {Payload}", task.Id, task.DataPayload);
                
                using var doc = System.Text.Json.JsonDocument.Parse(task.DataPayload);
                
                // 尝试不区分大小写查找 Title
                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    _logger.LogDebug("发现属性: {PropertyName} = {PropertyValue}", property.Name, property.Value);
                    
                    if (property.Name.Equals("Title", StringComparison.OrdinalIgnoreCase))
                    {
                        var title = property.Value.GetString();
                        if (!string.IsNullOrEmpty(title))
                        {
                            _logger.LogDebug("成功提取 Title: {Title}", title);
                            return title;
                        }
                    }
                }
                
                _logger.LogWarning("任务 {TaskId} 的 DataPayload 中未找到 Title 字段", task.Id);
            }
            else
            {
                _logger.LogDebug("任务 {TaskId} 的 DataPayload 为空", task.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析任务 {TaskId} 的 DataPayload 失败", task.Id);
        }

        // 如果没有 Title，返回任务ID
        return $"任务 #{task.Id}";
    }

    private string GetTaskCategoryName(TaskCategory category)
    {
        return category switch
        {
            TaskCategory.Immediate => "即刻执行",
            TaskCategory.Duration => "持续执行",
            TaskCategory.ResultPending => "结果等待",
            TaskCategory.DataCollection => "数据采集",
            TaskCategory.Verification => "核对用药",
            TaskCategory.ApplicationWithPrint => "检查申请",
            TaskCategory.DischargeConfirmation => "出院确认",
            _ => "其他任务"
        };
    }
}
