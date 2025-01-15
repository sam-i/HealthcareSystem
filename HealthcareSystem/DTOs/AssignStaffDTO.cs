namespace HealthcareSystem.DTOs
{
    public class AssignStaffDto
    {
        public int PatientId { get; set; }
        public int? DoctorId { get; set; }        // Nullable since assignment is optional
        public int? RadiologistId { get; set; }   // Nullable since assignment is optional
    }
}
