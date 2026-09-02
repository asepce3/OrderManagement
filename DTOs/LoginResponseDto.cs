namespace OrderManagement.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
