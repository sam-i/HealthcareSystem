namespace HealthcareSystem.ViewModels
{
    public class MedicalImageViewModel
    {
        public int Id { get; set; }
        public string StoragePath { get; set; }
        public string ImageType { get; set; }
        public string DiseaseCategory { get; set; }
        public string UploadDate { get; set; }
        public string Notes { get; set; }
        public decimal Cost { get; set; }
    }

    public class UpdateImageDetailsViewModel
    {
        public int ImageId { get; set; }
        public string DiseaseCategory { get; set; }
        public string Notes { get; set; }
    }
}