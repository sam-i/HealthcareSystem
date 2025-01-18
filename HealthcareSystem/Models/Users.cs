using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthcareSystem.Models
{
    public class Users
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public int Role { get; set; }
        public string Name { get; set; }
    }
    public enum UserRole
    {
        Administrator = 1,
        Doctor = 2,
        Radiologist = 3,
        Patient = 4
    }
}