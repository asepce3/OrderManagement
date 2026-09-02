using OrderManagement.Common;
using OrderManagement.DTOs;

namespace OrderManagement.Services;

public interface IAuthService
{
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto dto);
    Task<Result<RegisterResponseDto?>> RegisterAsync(RegisterRequestDto dto);
    Task<Result<RegisterResponseDto?>> CreateAdminAsync(CreateAdminRequestDto dto);
}
