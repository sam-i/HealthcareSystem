using HealthcareSystem.DTOs;
using HealthcareSystem.Models;

namespace HealthcareSystem.ViewModels
{
    public class RadiologistDashboardViewModel
    {
        public Radiologists Radiologist { get; set; }
        public Patients SelectedPatient { get; set; }
        public MedicalImages MedicalImage { get; set; }
        public List<Patients> Patients { get; set; } = new List<Patients>();
    }
}
