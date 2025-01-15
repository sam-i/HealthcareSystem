using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.DTOs
{
    public class DiseaseClassificationDto
    {
        public int ImageId { get; set; }

        [Required]
        public string DiseaseType { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }
}
