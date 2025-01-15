using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HealthcareSystem.Data;
using HealthcareSystem.Models;
using HealthcareSystem.ViewModels;
using HealthcareSystem.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace HealthcareSystem.Controllers
{
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            // Get the currently logged-in doctor
            var userId = int.Parse(User.Identity.Name);
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.PatientIds)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
            {
                return NotFound();
            }

            // Build the dashboard view model
            var viewModel = new DoctorDashboardViewModel
            {
                Name = doctor.User.Name,
                TotalPatients = doctor.PatientIds.Count,
                // Get count of diagnoses that need confirmation
                PendingDiagnoses = await _context.Diagnoses
                    .CountAsync(d => d.DiagnosedByDoctorId == doctor.Id),

                // Get list of doctor's patients
                Patients = await _context.Patients
                    .Where(p => p.AssignedDoctorId == doctor.Id)
                    .Select(p => new PatientListViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        CurrentCondition = p.CurrentCondition,
                        LastVisit = p.LastVisit
                    })
                    .ToListAsync(),

                // Get 5 most recent diagnoses
                RecentDiagnoses = await _context.Diagnoses
                    .Where(d => d.DiagnosedByDoctorId == doctor.Id)
                    .OrderByDescending(d => d.DiagnosisDate)
                    .Take(5)
                    .Select(d => new DiagnosisDto
                    {
                        PatientId = d.Id,
                        DiseaseType = d.DiseaseType,
                        Notes = d.Notes,
                        DiagnosisDate = d.DiagnosisDate,
                        PatientName = d.Patient.Name,
                        DoctorName = doctor.User.Name // Using the logged-in doctor's name
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDiagnosis([FromBody] DiagnosisDto dto)
        {
            var userId = int.Parse(User.Identity.Name);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
            {
                return NotFound("Doctor not found");
            }

            var diagnosis = new Diagnoses
            {
                DiseaseType = dto.DiseaseType,
                Notes = dto.Notes,
                DiagnosisDate = DateTime.UtcNow,
                PatientId = dto.PatientId,  // You might want to rename this in the DTO to PatientId for clarity
                DiagnosedByDoctorId = doctor.Id,
            };

            _context.Diagnoses.Add(diagnosis);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Diagnosis created successfully" });
        }

        public async Task<IActionResult> GetPatientDetails(int id)
        {
            var userId = int.Parse(User.Identity.Name);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
            {
                return NotFound("Doctor not found");
            }

            var patient = await _context.Patients
                .Include(p => p.MedicalImagesIds)
                .Include(p => p.DiagnosesIds)
                .FirstOrDefaultAsync(p => p.Id == id && p.AssignedDoctorId == doctor.Id);

            if (patient == null)
            {
                return NotFound("Patient not found");
            }

            return View(patient);
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