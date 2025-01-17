using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthcareSystem.Data;
using HealthcareSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using HealthcareSystem.ViewModels;

namespace HealthcareSystem.Controllers
{
    public class UpdateImageDetailsViewModel
    {
        public int ImageId { get; set; }
        public string DiseaseCategory { get; set; }
        public string Notes { get; set; }
    }
    public class UpdatePatientConditionViewModel
    {
        public int PatientId { get; set; }
        public string Condition { get; set; }
    }

    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.PatientIds)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
            {
                return NotFound();
            }

            var patients = await _context.Patients
                .Where(p => p.AssignedDoctorId == id)
                .Include(p => p.User)
                .Include(p => p.MedicalImages)
                .Include(p => p.AssignedRadiologist)
                    .ThenInclude(r => r.User)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var viewModel = new DoctorDashboardViewModel
            {
                Doctor = doctor,
                Patients = patients
            };

            var patientTasks = await _context.PatientTasks.Where(pt => patients.Select(p => p.Id).Contains(pt.PatientId)).ToListAsync();
            return View("~/Views/Home/DoctorDashboard.cshtml", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetPatientTasks(int patientId)
        {
            var tasks = await _context.PatientTasks
                .Where(t => t.PatientId == patientId)
                .Select(t => new  
                {
                    t.Id,
                    t.Description,
                    t.Date,
                    t.Status,
                    t.TaskCost,  
                    t.PatientId
                })
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            return Json(tasks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/json")]
        public async Task<IActionResult> AddPatientTask([FromBody] AddPatientTaskViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid input" });
                }

                var task = new PatientTasks
                {
                    PatientId = model.PatientId,
                    Description = model.Description,
                    TaskCost = model.TaskCost,
                    Date = DateTime.Now,
                    Status = (int)PatientTaskStatus.Pending
                };

                _context.PatientTasks.Add(task);

                var patient = await _context.Patients.FindAsync(model.PatientId);
                if (patient != null)
                {
                    patient.TotalCost += model.TaskCost;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Task added successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Error adding task: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/json")]
        public async Task<IActionResult> DeletePatientTask([FromBody] DeletePatientTaskViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var task = await _context.PatientTasks.FindAsync(model.TaskId);
                if (task == null)
                {
                    return NotFound(new { success = false, message = "Task not found" });
                }

                var patient = await _context.Patients.FindAsync(task.PatientId);
                if (patient != null)
                {
                    patient.TotalCost -= task.TaskCost;
                    if (patient.TotalCost < 0)
                    {
                        patient.TotalCost = 0; 
                    }
                }

                var medicalImage = await _context.MedicalImages.FirstOrDefaultAsync(m => m.PatientTaskId == model.TaskId);
                if (medicalImage != null)
                {
                    _context.MedicalImages.Remove(medicalImage);
                }

                _context.PatientTasks.Remove(task);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Json(new { success = true, message = "Task and associated medical image deleted successfully", medicalImageId = task.MedicalImageId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = $"Error deleting task: {ex.Message}" });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/json")]
        public async Task<IActionResult> EditPatientTask([FromBody] EditPatientTaskViewModel model)
        {
            try
            {
                var task = await _context.PatientTasks.FindAsync(model.TaskId);
                if (task == null)
                {
                    return Json(new { success = false, message = "Task not found" });
                }

                var patient = await _context.Patients.FindAsync(task.PatientId);
                if (patient != null)
                {
                    patient.TotalCost -= task.TaskCost;  
                    patient.TotalCost += model.TaskCost; 
                    if(patient.TotalCost < 0)
                    {
                        patient.TotalCost = 0;
                    }
                }

                task.Description = model.Description;
                task.TaskCost = model.TaskCost;
                task.Status = model.Status;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Task updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating task: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPatientImages(int patientId)
        {
            var images = await _context.MedicalImages.Where(m => m.PatientId == patientId).OrderByDescending(m => m.UploadDate).ToListAsync();
            return Json(images);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/json")]
        public async Task<IActionResult> UpdateImageDetails([FromBody] UpdateImageDetailsViewModel model)  
        {
            try 
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid data submitted" });
                }

                var image = await _context.MedicalImages.FindAsync(model.ImageId);
                if (image == null)
                {
                    return Json(new { success = false, message = "Image not found" });
                }

                image.DiseaseCategory = model.DiseaseCategory;
                image.Notes = model.Notes;
                image.IsClassified = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Image details updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating image: {ex}"); 
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePatientCondition([FromBody] UpdatePatientConditionViewModel model)
        {
            try
            {
                var patient = await _context.Patients.FindAsync(model.PatientId);
                if (patient == null)
                {
                    return Json(new { success = false, message = "Patient not found" });
                }

                patient.CurrentCondition = model.Condition;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Patient condition updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPatientRadiologist(int patientId)
        {
            var patient = await _context.Patients.Include(p => p.AssignedRadiologist).ThenInclude(r => r.User).FirstOrDefaultAsync(p => p.Id == patientId);
            if (patient?.AssignedRadiologist != null)
            {
                return Json(new { radiologistName = patient.AssignedRadiologist.User.Name });
            }
            return Json(new { radiologistName = "" });
        }

        [HttpGet]
        public async Task<IActionResult> GeneratePatientReport(int patientId)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.AssignedDoctor)
                    .ThenInclude(d => d.User)
                .Include(p => p.AssignedRadiologist)
                    .ThenInclude(r => r.User)
                .Include(p => p.MedicalImages)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
            {
                return Json(new { success = false, message = "Patient not found" });
            }

            var tasks = await _context.PatientTasks
                .Where(t => t.PatientId == patientId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            var images = await _context.MedicalImages
                .Where(m => m.PatientId == patientId)
                .OrderByDescending(m => m.UploadDate)
                .ToListAsync();

            var reportData = new
            {
                patientInfo = new
                {
                    name = patient.Name,
                    address = patient.Address,
                    condition = patient.CurrentCondition,
                    doctor = patient.AssignedDoctor?.User?.Name,
                    radiologist = patient.AssignedRadiologist?.User?.Name,
                    totalCost = patient.TotalCost
                },
                images = images.Select(img => new
                {
                    type = ((ImageType)img.ImageType).ToString(),
                    uploadDate = img.UploadDate,
                    diseaseCategory = img.DiseaseCategory,
                    notes = img.Notes,
                    cost = img.Cost,
                    storagePath = img.StoragePath
                }),
                tasks = tasks.Select(task => new
                {
                    date = task.Date,
                    description = task.Description,
                    status = ((PatientTaskStatus)task.Status).ToString(),
                    cost = task.TaskCost
                }),
                reportDate = DateTime.Now
            };

            return Json(new { success = true, data = reportData });
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }
    }
}