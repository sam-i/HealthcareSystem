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

        public string? CurrentCondition { get; set; }
        public int? AssignedDoctorId { get; set; }
        public int? AssignedRadiologistId { get; set; }
    }

    public class AddRadiologistViewModel
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
