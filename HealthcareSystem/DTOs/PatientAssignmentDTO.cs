namespace HealthcareSystem.DTOs
{
    public class PatientAssignmentDto
    {
        public int PatientId { get; set; }
        public int? DoctorId { get; set; }
        public int? RadiologistId { get; set; }
    }
}
