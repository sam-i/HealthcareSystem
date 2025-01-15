using System.ComponentModel.DataAnnotations;
using HealthcareSystem.Models;
using Microsoft.AspNetCore.Http;

namespace HealthcareSystem.DTOs
{
    public class ImageUploadDto
    {
        [Required]
        public IFormFile File { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public ImageType ImageType { get; set; }

        public string? Notes { get; set; }
    }

    // You might also want a response DTO for after the upload
    public class ImageUploadResponseDto
    {
        public int Id { get; set; }
        public string StoragePath { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}