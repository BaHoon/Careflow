using CareFlow.Application.DTOs.Staff;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;

namespace CareFlow.Application.Services;

public class StaffService
{
    private readonly IRepository<StaffBase, Guid> _staffRepo;
    private readonly IRepository<Doctor, Guid> _docRepo;
    private readonly IRepository<Nurse, Guid> _nurseRepo;
    private readonly IRepository<Department, int> _deptRepo;

    // 构造函数：注入所有需要的“仓库”
    public StaffService(
        IRepository<StaffBase, Guid> staffRepo,
        IRepository<Doctor, Guid> docRepo,
        IRepository<Nurse, Guid> nurseRepo,
        IRepository<Department, int> deptRepo)
    {
        _staffRepo = staffRepo;
        _docRepo = docRepo;
        _nurseRepo = nurseRepo;
        _deptRepo = deptRepo;
    }

    // ==========================================
    // 核心功能：获取所有人员列表
    // ==========================================
    public async Task<List<StaffResponseDto>> GetAllStaffAsync()
    {
        // 1. 查出所有基础员工信息
        var staffs = await _staffRepo.ListAsync();
        
        // 2. 查出所有辅助信息 (科室、医生详情、护士详情)
        // 提示：数据量小时直接查全表在内存匹配是最快的开发方式
        var depts = await _deptRepo.ListAsync();
        var doctors = await _docRepo.ListAsync();
        var nurses = await _nurseRepo.ListAsync();

        var result = new List<StaffResponseDto>();

        foreach (var s in staffs)
        {
            // A. 找科室名
            var dept = depts.FirstOrDefault(d => d.Id == s.DeptId);
            
            // B. 找职称/职级
            string displayTitle = "普通员工";

            if (s.StaffType == StaffType.Doctor)
            {
                var doc = doctors.FirstOrDefault(d => d.Id == s.Id);
                // 把枚举转成中文，如 DoctorTitle.Chief -> "Chief" (前端可以再翻译，或者后端直接返回中文)
                displayTitle = doc?.Title.ToString() ?? "未知医生";
            }
            else if (s.StaffType == StaffType.Nurse)
            {
                var nurse = nurses.FirstOrDefault(n => n.Id == s.Id);
                displayTitle = nurse?.Rank.ToString() ?? "未知护士";
            }
            else if (s.StaffType == StaffType.Admin)
            {
                displayTitle = "系统管理员";
            }

            // C. 组装 DTO
            result.Add(new StaffResponseDto
            {
                Id = s.Id,
                EmployeeNumber = s.EmployeeNumber,
                FullName = s.FullName,
                Role = s.StaffType.ToString(), // "Doctor", "Nurse"
                DeptName = dept?.DeptName ?? "未知科室",
                IsActive = s.IsActive,
                TitleOrRank = displayTitle
            });
        }

        return result;
    }
}