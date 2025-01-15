using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthcareSystem.Models
{
    public class PatientTasks
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public decimal Cost { get; set; }
        public DateTime Date { get; set; }
        public int Status { get; set; }
        public int PatientId { get; set; }
        public Patients Patient { get; set; }
        public decimal TaskCost { get; set; }
    }

    public enum PatientTaskStatus
    {
        Cancelled = -1,
        Pending = 0,
        InProgress = 1,
        Completed = 2
    }
}