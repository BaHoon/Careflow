using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Enums;

namespace CareFlow.Core.Models;

public class Medication : SoftDeleteEntity<int>
{
    public string DrugName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public DrugCategory Category { get; set; }
    public string Specification { get; set; } = string.Empty; // 规格
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}