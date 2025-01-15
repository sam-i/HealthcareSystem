using HealthcareSystem.DTOs;

namespace HealthcareSystem.ViewModels
{
    public class DoctorDashboardViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int TotalPatients { get; set; }
        public int PendingDiagnoses { get; set; }
        public List<PatientListViewModel> Patients { get; set; } = new();
        public List<DiagnosisDto> RecentDiagnoses { get; set; } = new();
    }
}
