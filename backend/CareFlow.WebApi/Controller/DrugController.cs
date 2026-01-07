using CareFlow.Application.DTOs.Drug;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DrugController : ControllerBase
{
    private readonly IRepository<Drug, string> _drugRepository;
    private readonly ILogger<DrugController> _logger;

    public DrugController(
        IRepository<Drug, string> drugRepository,
        ILogger<DrugController> logger)
    {
        _drugRepository = drugRepository;
        _logger = logger;
    }

    /// <summary>
    /// 获取药品列表（支持搜索和分页）
    /// </summary>
    /// <param name="keyword">搜索关键词（药品名/简拼/条码）</param>
    /// <param name="page">页码（默认1）</param>
    /// <param name="pageSize">每页数量（默认100）</param>
    /// <param name="category">药品分类（可选）</param>
    [HttpGet("list")]
    public async Task<ActionResult<DrugListResponseDto>> GetDrugList(
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] string? category = null)
    {
        try
        {
            _logger.LogInformation("获取药品列表，关键词: {Keyword}, 页码: {Page}, 每页: {PageSize}", 
                keyword, page, pageSize);

            var query = _drugRepository.GetQueryable()
                .Where(d => !d.IsDeleted); // 排除软删除的药品

            // 关键词搜索
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var searchTerm = keyword.ToLower();
                query = query.Where(d => 
                    d.GenericName.ToLower().Contains(searchTerm) ||
                    (d.TradeName != null && d.TradeName.ToLower().Contains(searchTerm)) ||
                    (d.Manufacturer != null && d.Manufacturer.ToLower().Contains(searchTerm))
                );
            }

            // 分类过滤
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(d => d.Category == category);
            }

            // 获取总数
            var total = await query.CountAsync();

            // 分页
            var drugs = await query
                .OrderBy(d => d.GenericName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new DrugListResponseDto
            {
                Total = total,
                Items = drugs.Select(d => new DrugListDto
                {
                    Id = d.Id,
                    GenericName = d.GenericName,
                    TradeName = d.TradeName,
                    Specification = d.Specification,
                    Manufacturer = d.Manufacturer,
                    Barcode = null, // 数据库中暂无此字段
                    Pinyin = null, // 数据库中暂无此字段
                    Category = d.Category,
                    UnitPrice = d.UnitPrice,
                    PriceUnit = d.PriceUnit
                }).ToList()
            };

            _logger.LogInformation("成功获取 {Count} 个药品，共 {Total} 个", result.Items.Count, total);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取药品列表失败");
            return StatusCode(500, new { message = "获取药品列表失败: " + ex.Message });
        }
    }

    /// <summary>
    /// 根据ID获取药品详情
    /// </summary>
    [HttpGet("{drugId}")]
    public async Task<ActionResult<DrugListDto>> GetDrugDetail(string drugId)
    {
        try
        {
            var drug = await _drugRepository.GetByIdAsync(drugId);
            
            if (drug == null || drug.IsDeleted)
            {
                return NotFound(new { message = $"未找到ID为 {drugId} 的药品" });
            }

            var result = new DrugListDto
            {
                Id = drug.Id,
                GenericName = drug.GenericName,
                TradeName = drug.TradeName,
                Specification = drug.Specification,
                Manufacturer = drug.Manufacturer,
                Barcode = null, // 数据库中暂无此字段
                Pinyin = null, // 数据库中暂无此字段
                Category = drug.Category,
                UnitPrice = drug.UnitPrice
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取药品详情失败，ID: {DrugId}", drugId);
            return StatusCode(500, new { message = "获取药品详情失败: " + ex.Message });
        }
    }
}
