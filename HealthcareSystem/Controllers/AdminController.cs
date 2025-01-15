using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HealthcareSystem.Data;
using HealthcareSystem.Models;
using HealthcareSystem.ViewModels;
using HealthcareSystem.DTOs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthcareSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalPatients = await _context.Patients.CountAsync(),
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalRadiologists = await _context.Radiologists.CountAsync(),
                TotalSystemCost = await _context.Patients.SumAsync(t => t.TotalCost),

                // Fetch list of patients with their assigned doctor and radiologist
                Patients = await _context.Patients
                    .Include(p => p.AssignedDoctor)
                        .ThenInclude(d => d.User)
                    .Include(p => p.AssignedRadiologist)
                        .ThenInclude(r => r.User)
                    .Select(p => new PatientDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Address = p.Address,
                        AssignedDoctorName = p.AssignedDoctor.User.Name,
                        AssignedRadiologistName = p.AssignedRadiologist.User.Name,
                        TotalCost = p.TotalCost
                    })
                    .ToListAsync(),

                // Fetch doctors with their patient count
                Doctors = await _context.Doctors
                    .Include(d => d.User)
                    .Include(d => d.PatientIds)
                    .Select(d => new DoctorSummaryDto
                    {
                        Id = d.Id,
                        Name = d.User.Name,
                        PatientCount = d.PatientIds.Count
                    })
                    .ToListAsync(),

                // Fetch radiologists with their patient count
                Radiologists = await _context.Radiologists
                    .Include(r => r.User)
                    .Include(r => r.PatientIds)
                    .Select(r => new RadiologistSummaryDto
                    {
                        Id = r.Id,
                        Name = r.User.Name,
                        PatientCount = r.PatientIds.Count
                    })
                    .ToListAsync()
            };

            return View("~/Views/Home/AdminDashboard.cshtml", viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateAccountDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            {
                return BadRequest("Username already exists");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Hash the password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var user = new Users
                {
                    Username = dto.Username,
                    PasswordHash = hashedPassword,  // Store hashed password
                    Role = dto.Role,
                    Name = dto.Name
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create corresponding role-specific record
                switch (dto.Role)
                {
                    case 2: // Doctor
                        var doctor = new Doctors
                        {
                            UserId = user.Id,
                            Name = dto.Name,
                            PatientIds = new List<Patients>(),  // Initialize empty collection
                            DiagnosesIds = new List<Diagnoses>() // Initialize empty collection
                        };
                        _context.Doctors.Add(doctor);
                        break;

                    case 3: // Radiologist
                        var radiologist = new Radiologists
                        {
                            UserId = user.Id,
                            Name = dto.Name,
                            PatientIds = new List<Patients>(), // Initialize empty collection
                            MedicalImagesIds = new List<MedicalImages>() // Initialize empty collection
                        };
                        _context.Radiologists.Add(radiologist);
                        break;

                    case 4: // Patient
                        var patient = new Patients
                        {
                            UserId = user.Id,
                            Name = dto.Name,
                            Address = dto.Address,
                            MedicalImagesIds = "", // Initialize empty collection
                            DiagnosesIds = "", // Initialize empty collection
                            TotalCost = 0 // Initialize cost
                        };
                        _context.Patients.Add(patient);
                        break;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { message = "User created successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest($"Error creating user: {ex.Message}");
            }
        }
        [HttpPost]
        public async Task<IActionResult> AddDoctor(AddDoctorViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create CreateAccountDto from the model
                var createAccountDto = new CreateAccountDto
                {
                    Username = model.Username,
                    Password = model.Password, // Note: In production, implement proper password hashing
                    Name = model.Name,
                    Role = 2 // Doctor role
                };

                // Use existing CreateUser method to create both User and Doctor records
                var result = await CreateUser(createAccountDto);

                if (result is OkObjectResult)
                {
                    // If patients were selected, assign them
                    if (model.PatientIds != null && model.PatientIds.Any())
                    {
                        var doctor = await _context.Doctors
                            .Include(d => d.User)
                            .FirstOrDefaultAsync(d => d.User.Username == model.Username);

                        if (doctor != null && model.PatientIds != null && model.PatientIds.Any())
                        {
                            var patients = await _context.Patients
                                .Where(p => model.PatientIds.Contains(p.Id))
                                .ToListAsync();

                            foreach (var patient in patients)
                            {
                                patient.AssignedDoctorId = doctor.Id;
                                doctor.PatientIds.Add(patient); // Add to doctor's collection
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
                    TempData["Success"] = "Doctor added successfully";
                }
                else
                {
                    TempData["Error"] = "Error adding doctor";
                }
            }
            return RedirectToAction("Dashboard", "Admin");
        }

        [HttpPost]
        public async Task<IActionResult> AddRadiologist(AddRadiologistViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create CreateAccountDto from the model
                var createAccountDto = new CreateAccountDto
                {
                    Username = model.Username,
                    Password = model.Password,
                    Name = model.Name,
                    Role = 3 // Radiologist role
                };

                // Use existing CreateUser method to create both User and Radiologist records
                var result = await CreateUser(createAccountDto);

                if (result is OkObjectResult)
                {
                    // If patients were selected, assign them
                    if (model.PatientIds != null && model.PatientIds.Any())
                    {
                        var radiologist = await _context.Radiologists
                            .Include(r => r.User)
                            .FirstOrDefaultAsync(r => r.User.Username == model.Username);

                        if (radiologist != null && model.PatientIds != null && model.PatientIds.Any())
                        {
                            var patients = await _context.Patients
                                .Where(p => model.PatientIds.Contains(p.Id))
                                .ToListAsync();

                            foreach (var patient in patients)
                            {
                                patient.AssignedRadiologistId = radiologist.Id;
                                radiologist.PatientIds.Add(patient);
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
                    TempData["Success"] = "Radiologist added successfully";
                }
                else
                {
                    TempData["Error"] = "Error adding radiologist";
                }
            }
            return RedirectToAction("Dashboard", "Admin");
        }

        [HttpPost]
        public async Task<IActionResult> AddPatient(AddPatientViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create CreateAccountDto from the model
                var createAccountDto = new CreateAccountDto
                {
                    Username = model.Username,
                    Password = model.Password,
                    Name = model.Name,
                    Role = 4, // Patient role
                    Address = model.Address // Make sure your CreateAccountDto includes this property
                };

                // Use existing CreateUser method to create both User and Patient records
                var result = await CreateUser(createAccountDto);

                if (result is OkObjectResult)
                {
                    // After patient is created, assign doctor and radiologist if specified
                    var patient = await _context.Patients
                        .FirstOrDefaultAsync(p => p.User.Username == model.Username);

                    if (patient != null)
                    {
                        patient.CurrentCondition = model.CurrentCondition;
                        patient.AssignedDoctorId = model.AssignedDoctorId;
                        patient.AssignedRadiologistId = model.AssignedRadiologistId;
                        patient.LastVisit = DateTime.Now;

                        await _context.SaveChangesAsync();
                    }

                    TempData["Success"] = "Patient added successfully";
                }
                else
                {
                    TempData["Error"] = "Error adding patient";
                }
            }
            return RedirectToAction("Dashboard", "Admin");
        }
        [HttpGet]
        public async Task<IActionResult> EditPatient(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.AssignedDoctor)
                .Include(p => p.AssignedRadiologist)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            var viewModel = new EditPatientViewModel
            {
                Id = patient.Id,
                Name = patient.Name,
                Address = patient.Address,
                CurrentCondition = patient.CurrentCondition,
                AssignedDoctorId = patient.AssignedDoctorId,
                AssignedRadiologistId = patient.AssignedRadiologistId,
                AvailableDoctors = await _context.Doctors
                    .Include(d => d.User)
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.User.Name
                    })
                    .ToListAsync(),
                AvailableRadiologists = await _context.Radiologists
                    .Include(r => r.User)
                    .Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = r.User.Name
                    })
                    .ToListAsync()
            };

            return View("~/Views/Home/EditPatient.cshtml", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditPatient(EditPatientViewModel model)
        {
            if (ModelState.IsValid)
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == model.Id);

                if (patient == null)
                {
                    return NotFound();
                }

                patient.Name = model.Name;
                patient.Address = model.Address;
                patient.CurrentCondition = model.CurrentCondition;
                patient.AssignedDoctorId = model.AssignedDoctorId;
                patient.AssignedRadiologistId = model.AssignedRadiologistId;

                // Update the associated user's name
                patient.User.Name = model.Name;

                await _context.SaveChangesAsync();
                return RedirectToAction("Dashboard", "Admin");
            }
            return View("~/Views/Home/EditPatient.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditDoctor(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.PatientIds)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
            {
                return NotFound();
            }

            var viewModel = new EditDoctorViewModel
            {
                Id = doctor.Id,
                Name = doctor.User.Name,
                AssignedPatientIds = doctor.PatientIds.Select(p => p.Id).ToList(),
                AvailablePatients = await _context.Patients
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Name
                    })
                    .ToListAsync()
            };

            return View("~/Views/Home/EditDoctor.cshtml", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditDoctor(EditDoctorViewModel model)
        {
            if (ModelState.IsValid)
            {
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .Include(d => d.PatientIds)
                    .FirstOrDefaultAsync(d => d.Id == model.Id);

                if (doctor == null)
                {
                    return NotFound();
                }

                doctor.User.Name = model.Name;

                // Update patient assignments
                var currentPatientIds = doctor.PatientIds.Select(p => p.Id).ToList();
                var patientsToRemove = currentPatientIds.Except(model.AssignedPatientIds ?? new List<int>());
                var patientsToAdd = (model.AssignedPatientIds ?? new List<int>()).Except(currentPatientIds);

                foreach (var patientId in patientsToRemove)
                {
                    var patient = await _context.Patients.FindAsync(patientId);
                    if (patient != null)
                    {
                        patient.AssignedDoctorId = null;
                    }
                }

                foreach (var patientId in patientsToAdd)
                {
                    var patient = await _context.Patients.FindAsync(patientId);
                    if (patient != null)
                    {
                        patient.AssignedDoctorId = doctor.Id;
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Dashboard", "Admin");
            }
            return View("~/Views/Home/EditDoctor.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditRadiologist(int id)
        {
            var radiologist = await _context.Radiologists
                .Include(r => r.User)
                .Include(r => r.PatientIds)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (radiologist == null)
            {
                return NotFound();
            }

            var viewModel = new EditRadiologistViewModel
            {
                Id = radiologist.Id,
                Name = radiologist.User.Name,
                AssignedPatientIds = radiologist.PatientIds.Select(p => p.Id).ToList(),
                AvailablePatients = await _context.Patients
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Name
                    })
                    .ToListAsync()
            };

            return View("~/Views/Home/EditRadiologist.cshtml", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditRadiologist(EditRadiologistViewModel model)
        {
            if (ModelState.IsValid)
            {
                var radiologist = await _context.Radiologists
                    .Include(r => r.User)
                    .Include(r => r.PatientIds)
                    .FirstOrDefaultAsync(r => r.Id == model.Id);

                if (radiologist == null)
                {
                    return NotFound();
                }

                radiologist.User.Name = model.Name;

                // Update patient assignments
                var currentPatientIds = radiologist.PatientIds.Select(p => p.Id).ToList();
                var patientsToRemove = currentPatientIds.Except(model.AssignedPatientIds ?? new List<int>());
                var patientsToAdd = (model.AssignedPatientIds ?? new List<int>()).Except(currentPatientIds);

                foreach (var patientId in patientsToRemove)
                {
                    var patient = await _context.Patients.FindAsync(patientId);
                    if (patient != null)
                    {
                        patient.AssignedRadiologistId = null;
                    }
                }

                foreach (var patientId in patientsToAdd)
                {
                    var patient = await _context.Patients.FindAsync(patientId);
                    if (patient != null)
                    {
                        patient.AssignedRadiologistId = radiologist.Id;
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Dashboard", "Admin");
            }
            return View("~/Views/Home/EditRadiologist.cshtml", model);
        }

        // Delete Methods
        [HttpPost]
        public async Task<IActionResult> DeletePatient(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient == null)
                {
                    return NotFound();
                }

                // Remove the user record
                if (patient.User != null)
                {
                    _context.Users.Remove(patient.User);
                }

                // Remove the patient record
                _context.Patients.Remove(patient);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok();
            }
            catch
            {
                await transaction.RollbackAsync();
                return BadRequest("Error deleting patient");
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .Include(d => d.PatientIds)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (doctor == null)
                {
                    return NotFound();
                }

                // Remove doctor assignments from patients
                foreach (var patient in doctor.PatientIds)
                {
                    patient.AssignedDoctorId = null;
                }

                // Remove the user record
                if (doctor.User != null)
                {
                    _context.Users.Remove(doctor.User);
                }

                // Remove the doctor record
                _context.Doctors.Remove(doctor);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok();
            }
            catch
            {
                await transaction.RollbackAsync();
                return BadRequest("Error deleting doctor");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRadiologist(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var radiologist = await _context.Radiologists
                    .Include(r => r.User)
                    .Include(r => r.PatientIds)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (radiologist == null)
                {
                    return NotFound();
                }

                // Remove radiologist assignments from patients
                foreach (var patient in radiologist.PatientIds)
                {
                    patient.AssignedRadiologistId = null;
                }

                // Remove the user record
                if (radiologist.User != null)
                {
                    _context.Users.Remove(radiologist.User);
                }

                // Remove the radiologist record
                _context.Radiologists.Remove(radiologist);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok();
            }
            catch
            {
                await transaction.RollbackAsync();
                return BadRequest("Error deleting radiologist");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                switch (user.Role)
                {
                    case 2: //doctor
                        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                        if (doctor != null)
                            _context.Doctors.Remove(doctor);
                        break;

                    case 3: //radiologist
                        var radiologist = await _context.Radiologists.FirstOrDefaultAsync(r => r.UserId == userId);
                        if (radiologist != null)
                            _context.Radiologists.Remove(radiologist);
                        break;
                    case 4: //patient
                        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
                        if (patient != null)
                            _context.Patients.Remove(patient);
                        break;
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { message = "User deleted successfully" });
            }
            catch
            {
                await transaction.RollbackAsync();
                return BadRequest("Error deleting user");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSystemStats()
        {
            var stats = new AdminDashboardViewModel
            {
                TotalPatients = await _context.Patients.CountAsync(),
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalRadiologists = await _context.Radiologists.CountAsync(),
                TotalSystemCost = await _context.Patients.SumAsync(t => t.TotalCost)
            };

            return Ok(stats);
        }
        [HttpPost]
        public IActionResult Logout()
        {
            // Clear the authentication cookie (sign out)
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect to the login page or home page
            return RedirectToAction("Login", "Auth");
        }

    }
}