using HealthcareSystem.Data;
using HealthcareSystem.Models;
using HealthcareSystem.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthcareSystem.Controllers
{
    public class RadiologistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<RadiologistController> _logger;

        public RadiologistController(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<RadiologistController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard(int id)
        {
            var radiologist = await _context.Radiologists
                .Include(r => r.PatientIds)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (radiologist == null)
            {
                return NotFound();
            }

            var patients = await _context.Patients
                .Where(p => p.AssignedRadiologistId == radiologist.Id)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var viewModel = new RadiologistDashboardViewModel
            {
                Radiologist = radiologist,
                Patients = patients,
                MedicalImage = new MedicalImages()
            };

            return View("~/Views/Home/RadiologistDashboard.cshtml", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetPatientImages(int patientId)
        {
            try
            {
                var images = await _context.MedicalImages
                    .Where(m => m.PatientId == patientId)
                    .Select(m => new
                    {
                        m.Id,
                        m.StoragePath,
                        m.ImageType,
                        m.UploadDate,
                        m.Cost
                    })
                    .ToListAsync();

                return Json(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient images");
                return Json(new List<object>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(int patientId, int radiologistId, int imageType, decimal? cost, IFormFile imageFile)
        {
            try
            {
                if (patientId <= 0 || radiologistId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid patient or radiologist ID" });
                }

                if (imageFile == null || imageFile.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file uploaded" });
                }

                if (!cost.HasValue || cost.Value < 0)
                {
                    return BadRequest(new { success = false, message = "Invalid cost value" });
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".dcm" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { success = false, message = $"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", "medical-images");
                    Directory.CreateDirectory(uploadDirectory);

                    var fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadDirectory, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    var medicalImage = new MedicalImages
                    {
                        PatientId = patientId,
                        StoragePath = fileName,
                        ImageType = imageType,
                        UploadDate = DateTime.UtcNow,
                        UploadedByRadiologistId = radiologistId,
                        IsClassified = false,
                        Cost = cost,
                        PatientTaskId = null
                    };
                    _context.MedicalImages.Add(medicalImage);
                    await _context.SaveChangesAsync();

                    var patientTask = new PatientTasks
                    {
                        Description = "Pay for Image Scan Procedure",
                        TaskCost = cost ?? 0,
                        Date = DateTime.UtcNow,
                        Status = 0,
                        PatientId = patientId,
                        MedicalImageId = medicalImage.Id
                    };
                    _context.PatientTasks.Add(patientTask);
                    await _context.SaveChangesAsync();

                    medicalImage.PatientTaskId = patientTask.Id;
                    _context.MedicalImages.Update(medicalImage);

                    var patient = await _context.Patients.FindAsync(patientId);
                    if (patient != null)
                    {
                        patient.TotalCost += cost.Value;
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Image uploaded successfully",
                        image = new
                        {
                            id = medicalImage.Id,
                            storagePath = medicalImage.StoragePath,
                            imageType = medicalImage.ImageType,
                            uploadDate = medicalImage.UploadDate,
                            cost = medicalImage.Cost
                        }
                    });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "An error occurred while uploading the image: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId, int radiologistId)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var image = await _context.MedicalImages
                        .FirstOrDefaultAsync(m => m.Id == imageId && m.UploadedByRadiologistId == radiologistId);

                    if (image == null)
                    {
                        return Json(new { success = false, message = "Image not found or you are not authorized to delete this image." });
                    }

                    var patientId = image.PatientId;

                    var task = await _context.PatientTasks
                        .FirstOrDefaultAsync(t => t.MedicalImageId == imageId);
                    if (task != null)
                    {
                        _context.PatientTasks.Remove(task);
                    }

                    var filePath = Path.Combine(_environment.WebRootPath, "uploads", "medical-images", image.StoragePath);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    _context.MedicalImages.Remove(image);
                    await _context.SaveChangesAsync();

                    var patient = await _context.Patients.FindAsync(patientId);
                    if (patient != null)
                    {
                        patient.TotalCost -= image.Cost ?? 0;
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Image deleted successfully." });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting medical image - ImageId: {imageId}, RadiologistId: {radiologistId}");
                return Json(new { success = false, message = "An error occurred while deleting the image: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPatientDoctor(int patientId)
        {
            var patient = await _context.Patients
                .Include(p => p.AssignedDoctor)
                    .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient?.AssignedDoctor != null)
            {
                return Json(new { doctorName = patient.AssignedDoctor.User.Name });
            }

            return Json(new { doctorName = "" });
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }
    }
}