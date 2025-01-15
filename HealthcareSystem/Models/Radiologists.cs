using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthcareSystem.Models
{
    public class Radiologists
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public Users User { get; set; }
        public ICollection<Patients> PatientIds { get; set; } = new List<Patients>();
        public ICollection<MedicalImages> MedicalImagesIds { get; set; } = new List<MedicalImages>();
    }
}