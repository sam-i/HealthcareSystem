using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthcareSystem.Models
{
    public class MedicalImages
    {
        public int Id { get; set; }
        public string StoragePath { get; set; }
        public int ImageType { get; set; }
        public string? DiseaseCategory { get; set; }
        public DateTime UploadDate { get; set; }
        public int PatientId { get; set; }
        public Patients Patient { get; set; }
        public string? Notes { get; set; }
        public int UploadedByRadiologistId { get; set; }
        public Radiologists UploadedByRadiologist { get; set; }
        public bool IsClassified { get; set; }
        public DateTime? ClassificationDate { get; set; }
        public decimal? Cost { get; set; }

    }

    public enum ImageType
    {
        MRI = 1,
        CT = 2,
        XRay = 3
    }
}