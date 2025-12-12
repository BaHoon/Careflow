using CareFlow.Application.Interfaces;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Organization; 
using CareFlow.Core.Models.Space; 
using Microsoft.Extensions.Logging;

namespace CareFlow.Application.Services
{
    public class NurseAssignmentService : INurseAssignmentService
    {
        private readonly IRepository<Patient, string> _patientRepo;
        private readonly IRepository<Bed, string> _bedRepo;
        private readonly INurseScheduleRepository _scheduleRepo;
        
        //构造函数，赋初值
        public NurseAssignmentService(
            IRepository<Patient, string> patientRepo,
            IRepository<Bed, string> bedRepo,
            INurseScheduleRepository scheduleRepo)
        {
            _patientRepo = patientRepo;
            _bedRepo = bedRepo;
            _scheduleRepo = scheduleRepo;
        }


        // 构造函数注入...

        public async Task<string?> CalculateResponsibleNurseAsync(string patientId, DateTime orderTime)
        {
            // 1. 查病人 -> 查床位 -> 查病区
            var patient = await _patientRepo.GetByIdAsync(patientId);
            if (patient?.BedId == null) return null;
        
            var bed = await _bedRepo.GetByIdAsync(patient.BedId);
            if (bed == null) return null;

            // 2. 查排班
            var roster = await _scheduleRepo.GetNurseOnDutyAsync(bed.WardId, orderTime);
        
            // 3. 返回护士ID
            return roster?.StaffId;
        }
    }
}