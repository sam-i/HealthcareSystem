using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthcareSystem.Models;
using Microsoft.AspNetCore.Authorization;
using HealthcareSystem.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using HealthcareSystem.ViewModels;

namespace HealthcareSystem.Controllers
{
    public class EditProfileViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
    }
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(int id)
        {
            var patient = await _context.Patients.Include(p => p.User).Include(p => p.MedicalImages).FirstOrDefaultAsync(p => p.Id == id);
            if (patient == null)
            {
                return NotFound();
            }
            var assignedDoctor = await _context.Doctors.Where(d => d.Id == patient.AssignedDoctorId).Include(d => d.User).FirstOrDefaultAsync();
            var assignedRadiologist = await _context.Radiologists.Where(r => r.Id == patient.AssignedRadiologistId).Include(r => r.User).FirstOrDefaultAsync();
            var patientTasks = await _context.PatientTasks.Where(pt => pt.PatientId == id).ToListAsync();
            var viewModel = new PatientDashboardViewModel
            {
                Patient = patient,
                AssignedDoctor = assignedDoctor,
                AssignedRadiologist = assignedRadiologist,
                PatientTasks = patientTasks
            };
            return View("~/Views/Home/PatientDashboard.cshtml", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile([FromBody] EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var patient = await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == model.Id);
                if (patient == null)
                {
                    return NotFound("Patient not found");
                }
                patient.Name = model.Name;
                patient.Address = model.Address;
                if (patient.User != null)
                {
                    patient.User.Name = model.Name;
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = $"Error updating profile: {ex.Message}" });
            }
        }

        private decimal CalculateTotalCosts(Patients patient)
        {
            return patient.MedicalImages?.Sum(m => m.Cost ?? 0) ?? 0;
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }
    }
}