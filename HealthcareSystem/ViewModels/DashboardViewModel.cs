using HealthcareSystem.DTOs;
using HealthcareSystem.Models;

namespace HealthcareSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalRadiologists { get; set; }
        public decimal TotalSystemCost { get; set; }
        public List<PatientDto> Patients { get; set; }
        public List<DoctorSummaryDto> Doctors { get; set; }
        public List<RadiologistSummaryDto> Radiologists { get; set; }
    }
    public class DoctorDashboardViewModel
    {
        public Doctors Doctor { get; set; }
        public List<Patients> Patients { get; set; } = new List<Patients>();
    }
    public class RadiologistDashboardViewModel
    {
        public Radiologists Radiologist { get; set; }
        public MedicalImages MedicalImage { get; set; }
        public List<Patients> Patients { get; set; } = new List<Patients>();
    }
    public class PatientDashboardViewModel
    {
        public HealthcareSystem.Models.Patients Patient { get; set; }
        public HealthcareSystem.Models.Doctors AssignedDoctor { get; set; }
        public HealthcareSystem.Models.Radiologists AssignedRadiologist { get; set; }
        public List<PatientTasks> PatientTasks { get; set; }
    }
}