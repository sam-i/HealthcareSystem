using HealthcareSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.ViewModels
{
    public class EditPatientViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; }

        public string? CurrentCondition { get; set; }

        public int? AssignedDoctorId { get; set; }
        public int? AssignedRadiologistId { get; set; }

        // For dropdowns in the view
        public List<SelectListItem> AvailableDoctors { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableRadiologists { get; set; } = new List<SelectListItem>();
        public Radiologists Radiologist { get; set; }
        public Patients Patient { get; set; }
        public MedicalImages MedicalImage { get; set; }
    }
}
