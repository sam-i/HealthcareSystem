using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.ViewModels
{
    public class AddPatientViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; }

        public string? CurrentCondition { get; set; }  // Optional
        public int? AssignedDoctorId { get; set; }    // Optional
        public int? AssignedRadiologistId { get; set; }
    }
}
