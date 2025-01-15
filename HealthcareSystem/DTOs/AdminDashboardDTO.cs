using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthcareSystem.DTOs
{
    public class AdminDashboardDto
    {
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalRadiologists { get; set; }
        public decimal TotalSystemCost { get; set; }
        public List<PatientDto> Patients { get; set; }
        public List<DoctorSummaryDto> Doctors { get; set; }
        public List<RadiologistSummaryDto> Radiologists { get; set; }
    }
}