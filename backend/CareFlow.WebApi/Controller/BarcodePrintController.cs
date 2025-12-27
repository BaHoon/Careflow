using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Nursing;
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
            // 获取条形码文件目录
            var barcodeDir = Path.Combine(_environment.WebRootPath, "barcodes", "ExecutionTasks");
            
            if (!Directory.Exists(barcodeDir))
            {
                return Ok(new
                {
                    success = true,
                    count = 0,
                    data = new List<object>()
                });
            }

            // 递归获取所有PNG文件
            var barcodeFiles = Directory.GetFiles(barcodeDir, "*.png", SearchOption.AllDirectories)
                .OrderByDescending(f => new FileInfo(f).CreationTime)
                .Take(100) // 限制最多返回100条
                .ToList();

            var result = new List<object>();

            foreach (var filePath in barcodeFiles)
            {
                try
                {
                    // 从文件名中提取任务ID：ExecutionTasks-{taskId}.png
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var parts = fileName.Split('-');
                    
                    if (parts.Length >= 2 && long.TryParse(parts[1], out long taskId))
                    {
                        // 查询任务信息
                        var task = await _taskRepository.GetQueryable()
                            .Include(t => t.Patient)
                                .ThenInclude(p => p.Bed)
                                    .ThenInclude(b => b.Ward)
                            .Include(t => t.MedicalOrder)
                            .FirstOrDefaultAsync(t => t.Id == taskId);

                        if (task == null) continue;

                        // 如果指定了病区，则筛选
                        if (!string.IsNullOrEmpty(wardId) && task.Patient?.Bed?.Ward?.Id != wardId)
                        {
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
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "处理条形码文件 {FilePath} 失败", filePath);
                    continue;
                }
            }

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
                var payload = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(task.DataPayload);
                if (payload != null && payload.ContainsKey("Title"))
                {
                    return payload["Title"].ToString() ?? "执行任务";
                }
            }
        }
        catch
        {
            // 解析失败，使用默认值
        }

        return $"任务 #{task.Id}";
    }
}
