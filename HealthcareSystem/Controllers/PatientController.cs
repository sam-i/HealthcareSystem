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
            var patient = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.MedicalImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            var assignedDoctor = await _context.Doctors
                .Where(d => d.Id == patient.AssignedDoctorId)
                .Include(d => d.User)
                .FirstOrDefaultAsync();

            var assignedRadiologist = await _context.Radiologists
                .Where(r => r.Id == patient.AssignedRadiologistId)
                .Include(r => r.User)
                .FirstOrDefaultAsync();

            var patientTasks = await _context.PatientTasks
                .Where(pt => pt.PatientId == id)
                .ToListAsync();

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
        public async Task<IActionResult> EditProfile([FromBody] Patients patientUpdate)
        {
            var existingPatient = await _context.Patients.FindAsync(patientUpdate.Id);
            if (existingPatient == null)
            {
                return NotFound();
            }

            try
            {
                // Update only the allowed fields
                existingPatient.Name = patientUpdate.Name;
                existingPatient.Address = patientUpdate.Address;

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PatientExists(patientUpdate.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        // Helper methods
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
            // Clear the authentication cookie (sign out)
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect to the login page or home page
            return RedirectToAction("Login", "Auth");
        }
    }
}