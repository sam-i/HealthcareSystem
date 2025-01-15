namespace HealthcareSystem.DTOs
{
    public class PatientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? CurrentCondition { get; set; }
        public int? AssignedDoctorId { get; set; }
        public string? AssignedDoctorName { get; set; }
        public int? AssignedRadiologistId { get; set; }
        public string? AssignedRadiologistName { get; set; }
        public decimal TotalCost { get; set; }
    }
}
