namespace HealthcareSystem.ViewModels
{
    public class PatientListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CurrentCondition { get; set; }
        public DateTime? LastVisit { get; set; }
        public string? RequiredImageTypes { get; set; }
        public DateTime? LastImageUpload { get; set; }
    }
}
