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
                Patients = await _context.Patients
                    .Include(p => p.User)
                    .Select(p => new PatientDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Address = p.Address,
                        AssignedDoctorName = _context.Doctors.Where(d => d.Id == p.AssignedDoctorId).Select(d => d.User.Name).FirstOrDefault(),
                        AssignedRadiologistName = _context.Radiologists.Where(r => r.Id == p.AssignedRadiologistId).Select(r => r.User.Name).FirstOrDefault(),
                        TotalCost = p.TotalCost
                    })
                    .OrderBy(p => p.Name)
                    .ToListAsync(),
                Doctors = await _context.Doctors
                    .Include(d => d.User)
                    .Include(d => d.PatientIds)
                    .Select(d => new DoctorSummaryDto
                    {
                        Id = d.Id,
                        Name = d.User.Name,
                        PatientCount = d.PatientIds.Count
                    })
                    .OrderBy(p => p.Name)
                    .ToListAsync(),

                Radiologists = await _context.Radiologists
                    .Include(r => r.User)
                    .Include(r => r.PatientIds)
                    .Select(r => new RadiologistSummaryDto
                    {
                        Id = r.Id,
                        Name = r.User.Name,
                        PatientCount = r.PatientIds.Count
                    })
                    .OrderBy(p => p.Name)
                    .ToListAsync()
            };
            return View("~/Views/Home/AdminDashboard.cshtml", viewModel);
        }

        private async Task<(bool success, string message, int? id)> CreateUserInternal(CreateAccountDto dto)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                {
                    return (false, "Username already exists", null);
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var user = new Users
                {
                    Username = dto.Username,
                    PasswordHash = hashedPassword,
                    Role = dto.Role,
                    Name = dto.Name
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                dynamic roleEntity = null;

                switch (dto.Role)
                {
                    case 2: // Doctor
                        roleEntity = new Doctors
                        {
                            UserId = user.Id,
                            Name = dto.Name
                        };
                        _context.Doctors.Add(roleEntity);
                        break;
                    case 3: // Radiologist
                        roleEntity = new Radiologists
                        {
                            UserId = user.Id,
                            Name = dto.Name
                        };
                        _context.Radiologists.Add(roleEntity);
                        break;
                    case 4: // Patient
                        roleEntity = new Patients
                        {
                            UserId = user.Id,
                            Name = dto.Name,
                            Address = dto.Address,
                            TotalCost = 0
                        };
                        _context.Patients.Add(roleEntity);
                        break;
                    default:
                        return (false, "Invalid role specified", null);
                }

                await _context.SaveChangesAsync();
                return (true, "User created successfully", roleEntity.Id);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating user: {ex.Message}", null);
            }
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
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                var user = new Users
                {
                    Username = dto.Username,
                    PasswordHash = hashedPassword,
                    Role = dto.Role,
                    Name = dto.Name
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync(); 
                dynamic roleEntity = null;
                switch (dto.Role)
                {
                    case 2: // Doctor
                        roleEntity = new Doctors
                        {
                            UserId = user.Id,
                            Name = dto.Name
                        };
                        _context.Doctors.Add(roleEntity);
                        break;
                    case 3: // Radiologist
                        roleEntity = new Radiologists
                        {
                            UserId = user.Id,
                            Name = dto.Name
                        };
                        _context.Radiologists.Add(roleEntity);
                        break;
                    case 4: // Patient
                        roleEntity = new Patients
                        {
                            UserId = user.Id,
                            Name = dto.Name,
                            Address = dto.Address,
                            TotalCost = 0
                        };
                        _context.Patients.Add(roleEntity);
                        break;
                    default:
                        await transaction.RollbackAsync();
                        return BadRequest("Invalid role specified");
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { id = roleEntity.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest($"Error creating user: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddPatient(AddPatientViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Dashboard");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var createAccountDto = new CreateAccountDto
                {
                    Username = model.Username,
                    Password = model.Password,
                    Name = model.Name,
                    Role = 4, // Patient role
                    Address = model.Address
                };
                var (success, message, patientId) = await CreateUserInternal(createAccountDto);
                if (!success)
                {
                    ModelState.AddModelError("", message);
                    return RedirectToAction("Dashboard");
                }
                var patient = await _context.Patients.FindAsync(patientId);
                if (patient != null)
                {
                    patient.CurrentCondition = model.CurrentCondition;
                    if (model.AssignedDoctorId.HasValue)
                    {
                        var doctor = await _context.Doctors.Include(d => d.PatientIds).FirstOrDefaultAsync(d => d.Id == model.AssignedDoctorId);
                        if (doctor != null)
                        {
                            patient.AssignedDoctorId = doctor.Id;
                            doctor.PatientIds.Add(patient);
                        }
                    }
                    if (model.AssignedRadiologistId.HasValue)
                    {
                        var radiologist = await _context.Radiologists.Include(r => r.PatientIds).FirstOrDefaultAsync(r => r.Id == model.AssignedRadiologistId);
                        if (radiologist != null)
                        {
                            patient.AssignedRadiologistId = radiologist.Id;
                            radiologist.PatientIds.Add(patient);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                await transaction.CommitAsync();
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Error adding patient: {ex.Message}");
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddDoctor(AddDoctorViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Dashboard");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var createAccountDto = new CreateAccountDto
                {
                    Username = model.Username,
                    Password = model.Password,
                    Name = model.Name,
                    Role = 2 // Doctor role
                };
                var (success, message, doctorId) = await CreateUserInternal(createAccountDto);
                if (!success)
                {
                    ModelState.AddModelError("", message);
                    return RedirectToAction("Dashboard");
                }
                var doctor = await _context.Doctors.Include(d => d.PatientIds).FirstOrDefaultAsync(d => d.Id == doctorId);
                if (doctor != null && model.PatientIds != null && model.PatientIds.Any())
                {
                    var patients = await _context.Patients.Where(p => model.PatientIds.Contains(p.Id)).ToListAsync();
                    foreach (var patient in patients)
                    {
                        patient.AssignedDoctorId = doctor.Id;
                        doctor.PatientIds.Add(patient);
                    }
                    await _context.SaveChangesAsync();
                }
                await transaction.CommitAsync();
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Error adding doctor: {ex.Message}");
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddRadiologist(AddRadiologistViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Dashboard");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var createAccountDto = new CreateAccountDto
                {
                    Username = model.Username,
                    Password = model.Password,
                    Name = model.Name,
                    Role = 3 // Radiologist role
                };
                var (success, message, radiologistId) = await CreateUserInternal(createAccountDto);
                if (!success)
                {
                    ModelState.AddModelError("", message);
                    return RedirectToAction("Dashboard");
                }
                var radiologist = await _context.Radiologists.Include(r => r.PatientIds).FirstOrDefaultAsync(r => r.Id == radiologistId);
                if (radiologist != null && model.PatientIds != null && model.PatientIds.Any())
                {
                    var patients = await _context.Patients.Where(p => model.PatientIds.Contains(p.Id)).ToListAsync();
                    foreach (var patient in patients)
                    {
                        patient.AssignedRadiologistId = radiologist.Id;
                        radiologist.PatientIds.Add(patient);
                    }
                    await _context.SaveChangesAsync();
                }
                await transaction.CommitAsync();
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Error adding radiologist: {ex.Message}");
                return RedirectToAction("Dashboard");
            }
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
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Name
                    })
                    .ToListAsync(),
                AvailableRadiologists = await _context.Radiologists
                    .Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = r.Name
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
                var patient = await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == model.Id);
                if (patient == null)
                {
                    return NotFound();
                }
                patient.Name = model.Name;
                patient.Address = model.Address;
                patient.CurrentCondition = model.CurrentCondition;
                patient.AssignedDoctorId = model.AssignedDoctorId;
                patient.AssignedRadiologistId = model.AssignedRadiologistId;
                patient.User.Name = model.Name;
                await _context.SaveChangesAsync();
                return RedirectToAction("Dashboard");
            }
            model.AvailableDoctors = await _context.Doctors
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                })
                .ToListAsync();
            model.AvailableRadiologists = await _context.Radiologists
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Name
                })
                .ToListAsync();

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
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
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
                    doctor.Name = model.Name;
                    var currentPatientIds = doctor.PatientIds.Select(p => p.Id).ToList();
                    var patientsToRemove = currentPatientIds.Except(model.AssignedPatientIds ?? new List<int>());
                    var patientsToAdd = (model.AssignedPatientIds ?? new List<int>()).Except(currentPatientIds);
                    foreach (var patientId in patientsToRemove)
                    {
                        var patient = await _context.Patients.FindAsync(patientId);
                        if (patient != null)
                        {
                            patient.AssignedDoctorId = null;
                            doctor.PatientIds.Remove(patient);
                        }
                    }
                    foreach (var patientId in patientsToAdd)
                    {
                        var patient = await _context.Patients.FindAsync(patientId);
                        if (patient != null)
                        {
                            patient.AssignedDoctorId = doctor.Id;
                            doctor.PatientIds.Add(patient);
                        }
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return RedirectToAction("Dashboard");
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return RedirectToAction("Dashboard");
                }
            }
            model.AvailablePatients = await _context.Patients
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                })
                .ToListAsync();
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
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
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
                    radiologist.Name = model.Name;
                    var currentPatientIds = radiologist.PatientIds.Select(p => p.Id).ToList();
                    var patientsToRemove = currentPatientIds.Except(model.AssignedPatientIds ?? new List<int>());
                    var patientsToAdd = (model.AssignedPatientIds ?? new List<int>()).Except(currentPatientIds);
                    foreach (var patientId in patientsToRemove)
                    {
                        var patient = await _context.Patients.FindAsync(patientId);
                        if (patient != null)
                        {
                            patient.AssignedRadiologistId = null;
                            radiologist.PatientIds.Remove(patient);
                        }
                    }
                    foreach (var patientId in patientsToAdd)
                    {
                        var patient = await _context.Patients.FindAsync(patientId);
                        if (patient != null)
                        {
                            patient.AssignedRadiologistId = radiologist.Id;
                            radiologist.PatientIds.Add(patient);
                        }
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return RedirectToAction("Dashboard");
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return RedirectToAction("Dashboard");
                }
            }
            model.AvailablePatients = await _context.Patients
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                })
                .ToListAsync();
            return View("~/Views/Home/EditRadiologist.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> DeletePatient(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var patient = await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
                if (patient == null)
                {
                    return NotFound();
                }
                if (patient.User != null)
                {
                    _context.Users.Remove(patient.User);
                }
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
                foreach (var patient in doctor.PatientIds)
                {
                    patient.AssignedDoctorId = null;
                }
                if (doctor.User != null)
                {
                    _context.Users.Remove(doctor.User);
                }
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
                foreach (var patient in radiologist.PatientIds)
                {
                    patient.AssignedRadiologistId = null;
                }
                if (radiologist.User != null)
                {
                    _context.Users.Remove(radiologist.User);
                }
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
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login", "Auth");
        }
    }
}