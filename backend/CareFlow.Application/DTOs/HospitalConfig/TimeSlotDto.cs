namespace CareFlow.Application.DTOs.HospitalConfig;

/// <summary>
/// 医院时段配置DTO
/// </summary>
public class TimeSlotDto
{
    public int Id { get; set; }
    public string SlotCode { get; set; } = null!;
    public string SlotName { get; set; } = null!;
    public string DefaultTime { get; set; } = null!; // 格式: "HH:mm:ss"
    public string? Description { get; set; }
}

/// <summary>
/// 给药途径DTO
/// </summary>
public class UsageRouteDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}
