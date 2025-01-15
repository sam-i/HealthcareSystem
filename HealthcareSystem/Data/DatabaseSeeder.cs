using Microsoft.EntityFrameworkCore;
using HealthcareSystem.Models;

namespace HealthcareSystem.Data
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            try
            {
                if (!_context.Users.Any())
                {
                    // Create Admin
                    var adminUser = new Users
                    {
                        Username = "admin",
                        PasswordHash = "admin123", // In production, hash this!
                        Role = 1,
                        Name = "System Admin"
                    };
                    _context.Users.Add(adminUser);

                    // Create a Doctor
                    var doctorUser = new Users
                    {
                        Username = "doctor",
                        PasswordHash = "doctor123",
                        Role = 2,
                        Name = "Dr. Smith"
                    };
                    _context.Users.Add(doctorUser);

                    var doctor = new Doctors
                    {
                        User = doctorUser
                    };
                    _context.Doctors.Add(doctor);

                    // Create a Radiologist
                    var radiologistUser = new Users
                    {
                        Username = "radiologist",
                        PasswordHash = "radio123",
                        Role = 3,
                        Name = "Dr. Johnson"
                    };
                    _context.Users.Add(radiologistUser);

                    var radiologist = new Radiologists
                    {
                        User = radiologistUser
                    };
                    _context.Radiologists.Add(radiologist);

                    // Create a Patient
                    var patientUser = new Users
                    {
                        Username = "patient",
                        PasswordHash = "patient123",
                        Role = 4,
                        Name = "John Doe"
                    };
                    _context.Users.Add(patientUser);

                    var patient = new Patients
                    {
                        User = patientUser,
                        Name = "John Doe",
                        Address = "123 Main St",
                        AssignedDoctorId = 1,
                        AssignedRadiologistId = 1
                    };
                    _context.Patients.Add(patient);

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while seeding the database.", ex);
            }
        }
    }
}