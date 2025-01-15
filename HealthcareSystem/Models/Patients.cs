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
        public string? MedicalImagesIds { get; set; }  // DB column
        public string? DiagnosesIds { get; set; }      // DB column
        public decimal TotalCost { get; set; }

        // Navigation properties
        public Users User { get; set; }
        public Doctors AssignedDoctor { get; set; }
        public Radiologists AssignedRadiologist { get; set; }
        public ICollection<Diagnoses> Diagnoses { get; set; } 
        public ICollection<MedicalImages> MedicalImages { get; set; }


        // Helper methods to convert string to list
        public List<int> GetMedicalImageIdsList()
        {
            if (string.IsNullOrEmpty(MedicalImagesIds))
                return new List<int>();

            return MedicalImagesIds.Split(',')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(int.Parse)
                .ToList();
        }

        public void SetMedicalImageIdsList(List<int> ids)
        {
            MedicalImagesIds = ids != null && ids.Any()
                ? string.Join(",", ids)
                : null;
        }

        // Helper methods to convert string to list for Diagnoses
        public List<int> GetDiagnosesIdsList()
        {
            if (string.IsNullOrEmpty(DiagnosesIds))
                return new List<int>();

            return DiagnosesIds.Split(',')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(int.Parse)
                .ToList();
        }

        public void SetDiagnosesIdsList(List<int> ids)
        {
            DiagnosesIds = ids != null && ids.Any()
                ? string.Join(",", ids)
                : null;
        }
    }
}