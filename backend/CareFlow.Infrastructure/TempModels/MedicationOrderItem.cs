using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class MedicationOrderItem
{
    public long Id { get; set; }

    public long MedicalOrderId { get; set; }

    public string DrugId { get; set; } = null!;

    public string Dosage { get; set; } = null!;

    public string Note { get; set; } = null!;

    public DateTime CreateTime { get; set; }

    public virtual Drug Drug { get; set; } = null!;

    public virtual MedicalOrder MedicalOrder { get; set; } = null!;
}
