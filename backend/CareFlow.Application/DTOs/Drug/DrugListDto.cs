namespace CareFlow.Application.DTOs.Drug;

/// <summary>
/// 药品列表项DTO
/// </summary>
public class DrugListDto
{
    public string Id { get; set; } = null!;
    public string GenericName { get; set; } = null!;
    public string? TradeName { get; set; }
    public string Specification { get; set; } = null!;
    public string? Manufacturer { get; set; }
    public string? Barcode { get; set; }
    public string? Pinyin { get; set; }
    public string? Category { get; set; }
    public decimal? UnitPrice { get; set; }
}

/// <summary>
/// 药品列表响应DTO
/// </summary>
public class DrugListResponseDto
{
    public int Total { get; set; }
    public List<DrugListDto> Items { get; set; } = new();
}
