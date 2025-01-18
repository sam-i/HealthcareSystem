using Microsoft.AspNetCore.Mvc;
using HealthcareSystem.Data;
using HealthcareSystem.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HealthcareSystem.Models;
using System.Diagnostics;

namespace HealthcareSystem.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View("~/Views/Home/Login.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/Login.cshtml", model);
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return View("~/Views/Home/Login.cshtml", model);
            }
            var token = GenerateJwtToken(user);
            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });
            switch(user.Role)
            {
                case 1: // Administrator
                    return RedirectToAction("Dashboard", "Admin");
                case 2: // Doctor
                    var doctor = await _context.Doctors.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (doctor != null)
                    {
                        return RedirectToAction("Dashboard", "Doctor", new { id = doctor.Id });
                    }
                    return RedirectToAction("Index", "Home");
                case 3: // Radiologist
                    var radiologist = await _context.Radiologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (radiologist != null)
                    {
                        return RedirectToAction("Dashboard", "Radiologist", new { id = radiologist.Id });
                    }
                    return RedirectToAction("Index", "Home");
                case 4: // Patient
                    var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (patient != null)
                    {
                        return RedirectToAction("Dashboard", "Patient", new { id = patient.Id });
                    }
                    return RedirectToAction("Index", "Home");
                default:
                    return RedirectToAction("Index", "Home"); 
            }
        }

        private string GenerateJwtToken(Users user)
        {
            var secret = _configuration["JWT:Secret"] ?? "000";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("UserId", user.Id.ToString())
            };
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return tokenString;
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}