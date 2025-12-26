using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class Drug
{
    public string Id { get; set; } = null!;

    public string GenericName { get; set; } = null!;

    public string? TradeName { get; set; }

    public string? Manufacturer { get; set; }

    public string Specification { get; set; } = null!;

    public string DosageForm { get; set; } = null!;

    public string? PackageSpec { get; set; }

    public string? AtcCode { get; set; }

    public string? Category { get; set; }

    public string AdministrationRoute { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public string PriceUnit { get; set; } = null!;

    public bool IsPrescriptionOnly { get; set; }

    public bool IsNarcotic { get; set; }

    public bool IsPsychotropic { get; set; }

    public bool IsAntibiotic { get; set; }

    public string Status { get; set; } = null!;

    public string? Indications { get; set; }

    public string? Contraindications { get; set; }

    public string? DosageInstructions { get; set; }

    public string? SideEffects { get; set; }

    public string? StorageConditions { get; set; }

    public int? ShelfLifeMonths { get; set; }

    public string? ApprovalNumber { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreateTime { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeleteTime { get; set; }

    public virtual ICollection<MedicationOrderItem> MedicationOrderItems { get; set; } = new List<MedicationOrderItem>();
}
