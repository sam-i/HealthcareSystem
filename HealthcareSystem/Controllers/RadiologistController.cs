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

        public RadiologistController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<RadiologistController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // GET: Radiologist/Dashboard/{id}
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
                .ToListAsync();

            var viewModel = new RadiologistDashboardViewModel
            {
                Radiologist = radiologist,
                Patients = patients,
                MedicalImage = new MedicalImages()
            };

            return View("~/Views/Home/RadiologistDashboard.cshtml", viewModel);
        }

        // GET: Radiologist/GetPatientImages?patientId={id}
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

        // POST: Radiologist/UploadImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(int patientId, int radiologistId, int imageType, decimal? cost, IFormFile imageFile)
        {
            try
            {
                _logger.LogInformation($"Upload attempt - PatientId: {patientId}, RadiologistId: {radiologistId}, ImageType: {imageType}, Cost: {cost}, File: {imageFile?.FileName}");

                // Validate input parameters
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

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".dcm" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { success = false, message = $"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}" });
                }

                // Start a transaction to ensure data consistency
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Ensure upload directory exists
                    var uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", "medical-images");
                    Directory.CreateDirectory(uploadDirectory);

                    // Generate unique filename
                    var fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadDirectory, fileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Create medical image record
                    var medicalImage = new MedicalImages
                    {
                        PatientId = patientId,
                        StoragePath = fileName,
                        ImageType = imageType,
                        UploadDate = DateTime.UtcNow,
                        UploadedByRadiologistId = radiologistId,
                        IsClassified = false,
                        Cost = cost
                    };

                    _context.MedicalImages.Add(medicalImage);
                    await _context.SaveChangesAsync();

                    // Update patient's total cost
                    var patient = await _context.Patients.FindAsync(patientId);
                    if (patient != null)
                    {
                        // Calculate total cost of all images for this patient
                        var totalImageCost = await _context.MedicalImages
                            .Where(m => m.PatientId == patientId)
                            .SumAsync(m => m.Cost ?? 0);

                        patient.TotalCost = totalImageCost;
                        await _context.SaveChangesAsync();
                    }

                    // Commit transaction
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
                catch (Exception)
                {
                    // Roll back transaction if anything fails
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading medical image - PatientId: {patientId}, RadiologistId: {radiologistId}");
                return BadRequest(new { success = false, message = "An error occurred while uploading the image: " + ex.Message });
            }
        }

        // POST: Radiologist/DeleteImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId, int radiologistId)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Find the image and validate permissions
                    var image = await _context.MedicalImages
                        .FirstOrDefaultAsync(m => m.Id == imageId && m.UploadedByRadiologistId == radiologistId);

                    if (image == null)
                    {
                        return Json(new { success = false, message = "Image not found or you are not authorized to delete this image." });
                    }

                    // Store patientId before deleting the image
                    var patientId = image.PatientId;

                    // Delete physical file if it exists
                    var filePath = Path.Combine(_environment.WebRootPath, "uploads", "medical-images", image.StoragePath);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    // Remove image record from database
                    _context.MedicalImages.Remove(image);
                    await _context.SaveChangesAsync();

                    // Update patient's total cost
                    var patient = await _context.Patients.FindAsync(patientId);
                    if (patient != null)
                    {
                        // Recalculate total cost for patient's remaining images
                        var totalImageCost = await _context.MedicalImages
                            .Where(m => m.PatientId == patientId)
                            .SumAsync(m => m.Cost ?? 0);

                        patient.TotalCost = totalImageCost;
                        await _context.SaveChangesAsync();
                    }

                    // Commit all changes
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Image deleted successfully." });
                }
                catch (Exception)
                {
                    // Roll back all changes if anything fails
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