using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderManagement.Common;
using OrderManagement.Data;
using OrderManagement.DTOs;
using OrderManagement.Exceptions;
using OrderManagement.Models;
using OrderManagement.Repositories;

namespace OrderManagement.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher, IConfiguration configuration, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto dto)
    {
        try
        {
            var user = await _userRepository.GetUserByEmailAsync(dto.Email);
            if (user == null)
                return Result<LoginResponseDto>.Failure("Invalid email or password.", StatusCodes.Status400BadRequest);

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return Result<LoginResponseDto>.Failure("Invalid email or password.", StatusCodes.Status400BadRequest);

            return Result<LoginResponseDto>.Success(GenerateToken(user));
        }
        catch (Exception e)
        {
            await _unitOfWork.RollbackAsync();
            Console.Write(e);
            return Result<LoginResponseDto>.Failure("An unexpected error occurred while logging in.", StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<Result<RegisterResponseDto?>> RegisterAsync(RegisterRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return Result<RegisterResponseDto?>.Failure("Email is required.", StatusCodes.Status400BadRequest);

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                return Result<RegisterResponseDto?>.Failure("Password must be at least 6 characters.", StatusCodes.Status400BadRequest);

            var existingUser = await _userRepository.GetUserByEmailAsync(dto.Email);
            if (existingUser != null)
                return Result<RegisterResponseDto?>.Failure("Email already registered.", StatusCodes.Status409Conflict);

            var user = new User
            {
                Email = dto.Email,
                Name = dto.Name,
                Address = dto.Address,
                Role = UserRoles.User,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            user.Password = _passwordHasher.HashPassword(user, dto.Password);

            await _unitOfWork.BeginTransactionAsync();
            await _userRepository.SaveUserAsync(user);
            await _unitOfWork.CommitAsync();

            return Result<RegisterResponseDto?>.Success(null);
        }
        catch (ConflictException e)
        {
            await _unitOfWork.RollbackAsync();
            Console.Write(e);
            return Result<RegisterResponseDto?>.Failure("Email already registered.", StatusCodes.Status409Conflict);
        }
        catch (Exception e)
        {
            await _unitOfWork.RollbackAsync();
            Console.Write(e);
            return Result<RegisterResponseDto?>.Failure("An unexpected error occurred while registering.", StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<Result<RegisterResponseDto?>> CreateAdminAsync(CreateAdminRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return Result<RegisterResponseDto?>.Failure("Email is required.", StatusCodes.Status400BadRequest);

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                return Result<RegisterResponseDto?>.Failure("Password must be at least 6 characters.", StatusCodes.Status400BadRequest);

            var existingUser = await _userRepository.GetUserByEmailAsync(dto.Email);
            if (existingUser != null)
                return Result<RegisterResponseDto?>.Failure("Email already registered.", StatusCodes.Status409Conflict);

            var user = new User
            {
                Email = dto.Email,
                Name = dto.Name,
                Address = dto.Address,
                Role = UserRoles.Admin,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            user.Password = _passwordHasher.HashPassword(user, dto.Password);

            await _unitOfWork.BeginTransactionAsync();
            await _userRepository.SaveUserAsync(user);
            await _unitOfWork.CommitAsync();

            return Result<RegisterResponseDto?>.Success(null);
        }
        catch (ConflictException e)
        {
            await _unitOfWork.RollbackAsync();
            Console.Write(e);
            return Result<RegisterResponseDto?>.Failure("Email already registered.", StatusCodes.Status409Conflict);
        }
        catch (Exception e)
        {
            await _unitOfWork.RollbackAsync();
            Console.Write(e);
            return Result<RegisterResponseDto?>.Failure("An unexpected error occurred while creating admin.", StatusCodes.Status500InternalServerError);
        }
    }

    private LoginResponseDto GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expireHours = int.Parse(jwtSettings["ExpireHours"]!);
        var expiresAt = DateTime.UtcNow.AddHours(expireHours);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new LoginResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email,
            Name = user.Name,
            ExpiresAt = expiresAt
        };
    }
}
