using CareFlow.Application.DTOs.Admin;
using CareFlow.Application.DTOs.Common;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Models.Space;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareFlow.Application.Services.Admin
{
    public class DepartmentManagementService : IDepartmentManagementService
    {
        private readonly ICareFlowDbContext _context;
        private readonly ISystemLogService _systemLogService;
        private readonly ILogger<DepartmentManagementService> _logger;

        public DepartmentManagementService(
            ICareFlowDbContext context,
            ISystemLogService systemLogService,
            ILogger<DepartmentManagementService> logger)
        {
            _context = context;
            _systemLogService = systemLogService;
            _logger = logger;
        }

        public async Task<PagedResultDto<DepartmentDto>> GetDepartmentListAsync(QueryDepartmentRequestDto request)
        {
            _logger.LogInformation("查询科室列表，筛选条件: {@Request}", request);

            var query = _context.Departments.AsQueryable();

            // 关键词搜索
            if (!string.IsNullOrEmpty(request.Keyword))
            {
                query = query.Where(d => 
                    d.DeptName.Contains(request.Keyword) || 
                    (d.Location != null && d.Location.Contains(request.Keyword)));
            }

            // 总数
            var total = await query.CountAsync();

            // 分页
            var departments = await query
                .OrderBy(d => d.CreateTime)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // 统计每个科室的病床数量
            var departmentIds = departments.Select(d => d.Id).ToList();
            var bedCounts = await _context.Beds
                .Where(b => departmentIds.Contains(b.Ward.DepartmentId))
                .GroupBy(b => b.Ward.DepartmentId)
                .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DepartmentId, x => x.Count);

            var items = departments.Select(d => new DepartmentDto
            {
                DepartmentId = d.Id,
                DepartmentName = d.DeptName,
                Location = d.Location,
                BedCount = bedCounts.ContainsKey(d.Id) ? bedCounts[d.Id] : 0,
                CreatedAt = d.CreateTime,
                UpdatedAt = null // EntityBase没有UpdatedAt字段，如需要可添加
            }).ToList();

            _logger.LogInformation("✅ 成功获取 {Count} 个科室", items.Count);

            return new PagedResultDto<DepartmentDto>
            {
                Items = items,
                TotalCount = total,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }

        public async Task<DepartmentDto?> GetDepartmentByIdAsync(string departmentId)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == departmentId);

            if (department == null)
                return null;

            // 统计病床数量
            var bedCount = await _context.Beds
                .Where(b => b.Ward.DepartmentId == departmentId)
                .CountAsync();

            return new DepartmentDto
            {
                DepartmentId = department.Id,
                DepartmentName = department.DeptName,
                Location = department.Location,
                BedCount = bedCount,
                CreatedAt = department.CreateTime
            };
        }

        public async Task<DepartmentDto> CreateDepartmentAsync(SaveDepartmentDto dto, string? operatorId = null, string? operatorName = null, string? ipAddress = null)
        {
            _logger.LogInformation("创建科室: {@Dto}", dto);

            // 生成科室ID（如果未提供）
            var departmentId = dto.DepartmentId ?? GenerateDepartmentId(dto.DepartmentName);

            // 检查ID是否已存在
            var exists = await _context.Departments.AnyAsync(d => d.Id == departmentId);
            if (exists)
            {
                throw new InvalidOperationException($"科室ID {departmentId} 已存在");
            }

            var department = new Department
            {
                Id = departmentId,
                DeptName = dto.DepartmentName,
                Location = dto.Location ?? string.Empty,
                CreateTime = DateTime.UtcNow
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            // 自动创建一个默认病区
            var defaultWard = new Ward
            {
                Id = $"{departmentId}-W01",
                DepartmentId = departmentId
            };
            _context.Wards.Add(defaultWard);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ 科室创建成功: {DepartmentId}, 已自动创建默认病区: {WardId}", departmentId, defaultWard.Id);

            // 记录系统日志
            await _systemLogService.LogAsync(new CreateSystemLogDto
            {
                OperationType = "DepartmentCreated",
                OperatorId = null,
                OperatorName = operatorName,
                OperationDetails = $"创建科室: {dto.DepartmentName} ({departmentId}), 位置: {dto.Location}",
                IpAddress = ipAddress,
                Result = "Success",
                EntityType = "Department",
                EntityId = 0
            });

            return new DepartmentDto
            {
                DepartmentId = department.Id,
                DepartmentName = department.DeptName,
                Location = department.Location,
                BedCount = 0, // 新科室没有病床
                CreatedAt = department.CreateTime
            };
        }

        public async Task<DepartmentDto> UpdateDepartmentAsync(string departmentId, SaveDepartmentDto dto, string? operatorId = null, string? operatorName = null, string? ipAddress = null)
        {
            _logger.LogInformation("更新科室 {DepartmentId}: {@Dto}", departmentId, dto);

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == departmentId);

            if (department == null)
            {
                throw new KeyNotFoundException($"科室 {departmentId} 不存在");
            }

            var oldName = department.DeptName;
            var oldLocation = department.Location;

            department.DeptName = dto.DepartmentName;
            department.Location = dto.Location ?? string.Empty;

            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ 科室更新成功: {DepartmentId}", departmentId);

            // 记录系统日志
            await _systemLogService.LogAsync(new CreateSystemLogDto
            {
                OperationType = "DepartmentModified",
                OperatorId = null,
                OperatorName = operatorName,
                OperationDetails = $"修改科室: {oldName} → {dto.DepartmentName}, 位置: {oldLocation} → {dto.Location}",
                IpAddress = ipAddress,
                Result = "Success",
                EntityType = "Department",
                EntityId = 0
            });

            // 统计病床数量
            var bedCount = await _context.Beds
                .Where(b => b.Ward.DepartmentId == departmentId)
                .CountAsync();

            return new DepartmentDto
            {
                DepartmentId = department.Id,
                DepartmentName = department.DeptName,
                Location = department.Location,
                BedCount = bedCount,
                CreatedAt = department.CreateTime
            };
        }

        public async Task<bool> DeleteDepartmentAsync(string departmentId, string? operatorId = null, string? operatorName = null, string? ipAddress = null)
        {
            _logger.LogInformation("删除科室 {DepartmentId}", departmentId);

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == departmentId);

            if (department == null)
            {
                return false;
            }

            // 检查该科室的病床数量是否为0
            var bedCount = await _context.Beds
                .Where(b => b.Ward.DepartmentId == departmentId)
                .CountAsync();

            if (bedCount > 0)
            {
                throw new InvalidOperationException($"该科室有 {bedCount} 张病床，只有病床数为0的科室才能删除");
            }

            var departmentName = department.DeptName;

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ 科室删除成功: {DepartmentId}", departmentId);

            // 记录系统日志
            await _systemLogService.LogAsync(new CreateSystemLogDto
            {
                OperationType = "DepartmentDeleted",
                OperatorId = null,
                OperatorName = operatorName,
                OperationDetails = $"删除科室: {departmentName} ({departmentId})",
                IpAddress = ipAddress,
                Result = "Success",
                EntityType = "Department",
                EntityId = 0
            });

            return true;
        }

        public async Task<List<DepartmentDto>> GetActiveDepartmentsAsync()
        {
            var departments = await _context.Departments
                .OrderBy(d => d.DeptName)
                .ToListAsync();

            // 统计每个科室的病床数量
            var departmentIds = departments.Select(d => d.Id).ToList();
            var bedCounts = await _context.Beds
                .Where(b => departmentIds.Contains(b.Ward.DepartmentId))
                .GroupBy(b => b.Ward.DepartmentId)
                .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DepartmentId, x => x.Count);

            return departments.Select(d => new DepartmentDto
            {
                DepartmentId = d.Id,
                DepartmentName = d.DeptName,
                Location = d.Location,
                BedCount = bedCounts.ContainsKey(d.Id) ? bedCounts[d.Id] : 0,
                CreatedAt = d.CreateTime
            }).ToList();
        }

        private string GenerateDepartmentId(string deptName)
        {
            // 简单的ID生成逻辑：取中文拼音首字母或使用时间戳
            // 这里使用简化版本
            return $"DEPT_{DateTime.Now:yyyyMMddHHmmss}";
        }
    }
}
