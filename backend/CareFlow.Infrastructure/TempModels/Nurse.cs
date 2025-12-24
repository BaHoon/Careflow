using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class Nurse
{
    public string Id { get; set; } = null!;

    public string NurseRank { get; set; } = null!;

    public virtual ICollection<ExecutionTask> ExecutionTaskAssignedNurses { get; set; } = new List<ExecutionTask>();

    public virtual ICollection<ExecutionTask> ExecutionTaskCompleterNurses { get; set; } = new List<ExecutionTask>();

    public virtual ICollection<ExecutionTask> ExecutionTaskExecutorStaffs { get; set; } = new List<ExecutionTask>();

    public virtual Staff IdNavigation { get; set; } = null!;

    public virtual ICollection<MedicalOrder> MedicalOrderNurses { get; set; } = new List<MedicalOrder>();

    public virtual ICollection<MedicalOrder> MedicalOrderRejectedByNurses { get; set; } = new List<MedicalOrder>();

    public virtual ICollection<MedicalOrder> MedicalOrderSignedByNurses { get; set; } = new List<MedicalOrder>();

    public virtual ICollection<MedicalOrder> MedicalOrderStopConfirmedByNurses { get; set; } = new List<MedicalOrder>();

    public virtual ICollection<MedicalOrder> MedicalOrderStopRejectedByNurses { get; set; } = new List<MedicalOrder>();

    public virtual ICollection<NurseRoster> NurseRosters { get; set; } = new List<NurseRoster>();

    public virtual ICollection<NursingCareNote> NursingCareNotes { get; set; } = new List<NursingCareNote>();

    public virtual ICollection<NursingTask> NursingTasks { get; set; } = new List<NursingTask>();

    public virtual ICollection<VitalSignsRecord> VitalSignsRecords { get; set; } = new List<VitalSignsRecord>();
}
