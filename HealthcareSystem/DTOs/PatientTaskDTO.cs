using HealthcareSystem.Models;

namespace HealthcareSystem.DTOs
{
    public class PatientTaskDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public DateTime Date { get; set; }
        public PatientTaskStatus Status { get; set; }
    }
}