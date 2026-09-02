using Microsoft.AspNetCore.Mvc;
using OrderManagement.Common;
using OrderManagement.DTOs;
using OrderManagement.Services;

namespace OrderManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (result.IsFailure)
            return StatusCode(result.Code, ApiResponse<LoginResponseDto>.ErrorResponse(result.Error!));

        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result.Data!));
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegisterResponseDto>>> Register(RegisterRequestDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        if (result.IsFailure)
            return StatusCode(result.Code, ApiResponse<RegisterResponseDto>.ErrorResponse(result.Error!));

        return Ok(ApiResponse<RegisterResponseDto>.SuccessResponse(result.Data!));
    }
}
