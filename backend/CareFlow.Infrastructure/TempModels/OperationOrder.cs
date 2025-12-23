using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class OperationOrder
{
    public long Id { get; set; }

    public string OpId { get; set; } = null!;

    public bool Normal { get; set; }

    public string FrequencyType { get; set; } = null!;

    public string FrequencyValue { get; set; } = null!;

    public virtual MedicalOrder IdNavigation { get; set; } = null!;
}
