using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthcareSystem.Models
{
    public class Diagnoses
    {
        public int Id { get; set; }
        public string DiseaseType { get; set; }
        public string Notes { get; set; }
        public DateTime DiagnosisDate { get; set; }
        public int PatientId { get; set; }
        public Patients Patient { get; set; }
        public int DiagnosedByDoctorId { get; set; }
        public Doctors DiagnosedByDoctor { get; set; }  
        public int? MedicalImageId { get; set; }
        public MedicalImages? MedicalImage { get; set; }
    }
}