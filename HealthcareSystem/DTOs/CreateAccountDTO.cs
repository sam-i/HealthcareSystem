namespace HealthcareSystem.DTOs
{
    public class CreateAccountDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Role { get; set; }
        public string? Address { get; set; }
    }
}