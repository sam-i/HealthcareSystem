using System;
using System.Collections.Generic;
using System.Linq;

namespace HealthcareSystem.Models
{
    public class Patients
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public int UserId { get; set; }
        public int? AssignedDoctorId { get; set; }
        public int? AssignedRadiologistId { get; set; }
        public string? CurrentCondition { get; set; }
        public DateTime? LastVisit { get; set; }
        public decimal TotalCost { get; set; }

        // Navigation properties
        public Users User { get; set; }
        public Doctors AssignedDoctor { get; set; }
        public Radiologists AssignedRadiologist { get; set; }
        public ICollection<Diagnoses> Diagnoses { get; set; } 
        public ICollection<MedicalImages> MedicalImages { get; set; }

        public Patients()
        {
            Diagnoses = new List<Diagnoses>();
            MedicalImages = new List<MedicalImages>();
        }

    }
}