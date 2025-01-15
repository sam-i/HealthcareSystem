using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.ViewModels
{
    public class AddDoctorViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        public List<int>? PatientIds { get; set; } 
    }
}
