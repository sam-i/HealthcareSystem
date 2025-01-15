using HealthcareSystem.Models;

namespace HealthcareSystem.DTOs
{
    public class ImageDetailsDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string PatientName { get; set;  }   = string.Empty;
        public ImageType Type { get; set; }
        public string? DiseaseCategory { get; set; }
        public DateTime UploadDate { get; set; }
        public string? Notes { get; set; }
        public bool IsClassified { get; set; }
    }
}
