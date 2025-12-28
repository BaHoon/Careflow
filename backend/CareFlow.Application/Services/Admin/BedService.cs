using CareFlow.Application.DTOs.Admin;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Models.Space;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CareFlow.Application.Services.Admin
{
    public class BedService : IBedService
    {
        private readonly ICareFlowDbContext _context;
        private readonly ILogger<BedService> _logger;

        public BedService(
            ICareFlowDbContext context,
            ILogger<BedService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<BedDto>> GetBedsByDepartmentIdAsync(string departmentId)
        {
            _logger.LogInformation("获取科室 {DepartmentId} 的所有病床", departmentId);

            var beds = await _context.Beds
                .Include(b => b.Ward)
                .ThenInclude(w => w.Department)
                .Where(b => b.Ward.DepartmentId == departmentId)
                .OrderBy(b => b.Id)
                .ToListAsync();

            var result = beds.Select(b => new BedDto
            {
                BedId = b.Id,
                WardId = b.WardId,
                WardName = b.Ward.Id,
                DepartmentId = b.Ward.DepartmentId,
                DepartmentName = b.Ward.Department.DeptName,
                Status = b.Status
            }).ToList();

            _logger.LogInformation("✅ 找到 {Count} 张病床", result.Count);
            return result;
        }

        public async Task<BedDto> CreateBedAsync(CreateBedDto dto)
        {
            _logger.LogInformation("为科室 {DepartmentId} 创建新病床", dto.DepartmentId);

            // 验证病区是否存在且属于该科室
            var ward = await _context.Wards
                .Include(w => w.Department)
                .FirstOrDefaultAsync(w => w.Id == dto.WardId && w.DepartmentId == dto.DepartmentId);

            if (ward == null)
            {
                throw new InvalidOperationException($"病区 {dto.WardId} 不存在或不属于科室 {dto.DepartmentId}");
            }

            // 获取该病区现有的所有病床
            var existingBeds = await _context.Beds
                .Where(b => b.WardId == dto.WardId)
                .OrderBy(b => b.Id)
                .Select(b => b.Id)
                .ToListAsync();

            // 生成新的病床ID
            string newBedId = GenerateNextBedId(dto.WardId, existingBeds);

            // 创建新病床
            var bed = new Bed
            {
                Id = newBedId,
                WardId = dto.WardId,
                Status = "空闲",
                CreateTime = DateTime.UtcNow
            };

            _context.Beds.Add(bed);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ 病床创建成功: {BedId}", newBedId);

            return new BedDto
            {
                BedId = bed.Id,
                WardId = bed.WardId,
                WardName = ward.Id,
                DepartmentId = ward.DepartmentId,
                DepartmentName = ward.Department.DeptName,
                Status = bed.Status
            };
        }

        public async Task<bool> DeleteBedAsync(string bedId)
        {
            _logger.LogInformation("删除病床 {BedId}", bedId);

            var bed = await _context.Beds.FirstOrDefaultAsync(b => b.Id == bedId);
            if (bed == null)
            {
                _logger.LogWarning("❌ 病床 {BedId} 不存在", bedId);
                return false;
            }

            // 检查病床是否被占用
            if (bed.Status == "占用")
            {
                throw new InvalidOperationException($"病床 {bedId} 当前被占用，无法删除");
            }

            // 检查是否有患者分配到该病床
            var hasPatient = await _context.Patients.AnyAsync(p => p.BedId == bedId);
            if (hasPatient)
            {
                throw new InvalidOperationException($"病床 {bedId} 有患者使用记录，无法删除");
            }

            _context.Beds.Remove(bed);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ 病床删除成功: {BedId}", bedId);
            return true;
        }

        public async Task<int> CountBedsByDepartmentIdAsync(string departmentId)
        {
            var count = await _context.Beds
                .Where(b => b.Ward.DepartmentId == departmentId)
                .CountAsync();

            return count;
        }

        /// <summary>
        /// 生成下一个病床ID
        /// 格式: {WardId}-{序号}，例如: IM-W01-001, IM-W01-002
        /// </summary>
        private string GenerateNextBedId(string wardId, List<string> existingBedIds)
        {
            if (existingBedIds.Count == 0)
            {
                return $"{wardId}-001";
            }

            // 从现有ID中提取最大序号
            int maxNumber = 0;
            var pattern = $@"^{Regex.Escape(wardId)}-(\d+)$";
            
            foreach (var bedId in existingBedIds)
            {
                var match = Regex.Match(bedId, pattern);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
                {
                    if (number > maxNumber)
                    {
                        maxNumber = number;
                    }
                }
            }

            // 生成新序号（最大序号+1）
            int nextNumber = maxNumber + 1;
            return $"{wardId}-{nextNumber:D3}";
        }
    }
}
