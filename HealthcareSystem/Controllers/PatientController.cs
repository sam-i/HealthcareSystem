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

namespace HealthcareSystem.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Patients/Dashboard/5
        [HttpGet]
        public async Task<IActionResult> Dashboard(int id)
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

            // Calculate total costs
            patient.TotalCost = CalculateTotalCosts(patient);

            return View("~/Views/Home/PatientDashboard.cshtml", patient);
        }

        // POST: Patients/EditProfile
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
            decimal totalCost = 0;

            var medicalImageIds = patient.GetMedicalImageIdsList();
            if (medicalImageIds.Any())
            {
                var imageCosts = _context.MedicalImages
                    .Where(m => medicalImageIds.Contains(m.Id))
                    .Sum(m => m.Cost.GetValueOrDefault());
                totalCost += imageCosts;
            }

            return totalCost;
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