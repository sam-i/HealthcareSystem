namespace HealthcareSystem.DTOs
{
    public class DoctorSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int PatientCount { get; set; }
        public List<PatientSummaryDto> Patients { get; set; }
    }
}
