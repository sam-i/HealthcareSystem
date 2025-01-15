namespace HealthcareSystem.DTOs
{
    public class DiagnosisDto
    {
        public int PatientId { get; set; }
        public string DiseaseType { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime DiagnosisDate { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
    }
}
