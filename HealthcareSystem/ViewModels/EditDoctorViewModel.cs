using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.ViewModels
{
    public class EditDoctorViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        public List<int>? AssignedPatientIds { get; set; }
        public List<SelectListItem> AvailablePatients { get; set; } = new List<SelectListItem>();
    }
}
