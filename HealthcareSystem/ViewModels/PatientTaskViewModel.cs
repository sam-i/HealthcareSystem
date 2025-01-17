namespace HealthcareSystem.ViewModels
{
    public class AddPatientTaskViewModel
    {
        public int PatientId { get; set; }
        public string Description { get; set; }
        public decimal TaskCost { get; set; }
        public DateTime Date { get; set; }
        public int Status { get; set; }
    }

    public class DeletePatientTaskViewModel
    {
        public int TaskId { get; set; }
        public string __RequestVerificationToken { get; set; }
    }

    public class EditPatientTaskViewModel
    {
        public int TaskId { get; set; }
        public int Status { get; set; }
        public string Description { get; set; }
        public decimal TaskCost { get; set; }
    }
}