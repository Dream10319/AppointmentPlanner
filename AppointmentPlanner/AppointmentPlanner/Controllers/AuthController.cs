using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AppointmentPlanner.Models.Identity;
using AppointmentPlanner.Services;
using System.Security.Claims;

namespace AppointmentPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<ApplicationRole> roleManager,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid registration data",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        // Validate role
        if (!UserRoles.AllRoles.Contains(model.Role))
        {
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid role specified",
                Errors = new List<string> { "The specified role is not valid." }
            });
        }

        // Create user
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = model.PhoneNumber,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "User registration failed",
                Errors = result.Errors.Select(e => e.Description).ToList()
            });
        }

        // Assign role
        await _userManager.AddToRoleAsync(user, model.Role);

        // Generate token
        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtService.GenerateToken(user, roles);

        return Ok(new AuthResponseDto
        {
            IsSuccess = true,
            Message = "User registered successfully",
            Token = token,
            ExpiresAt = _jwtService.GetTokenExpiration(),
            User = MapToUserInfoDto(user, roles)
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid login data",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            return Unauthorized(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid email or password"
            });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Your account has been deactivated. Please contact support."
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

        if (!result.Succeeded)
        {
            return Unauthorized(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid email or password"
            });
        }

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate token
        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtService.GenerateToken(user, roles);

        return Ok(new AuthResponseDto
        {
            IsSuccess = true,
            Message = "Login successful",
            Token = token,
            ExpiresAt = _jwtService.GetTokenExpiration(),
            User = MapToUserInfoDto(user, roles)
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid email",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Message = "If your email is registered, you will receive a password reset link."
            });
        }

        // Generate password reset token
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // In a real application, send an email with the token
        // For this demo, we'll just return the token in the response
        _logger.LogInformation($"Password reset token for {user.Email}: {token}");

        return Ok(new AuthResponseDto
        {
            IsSuccess = true,
            Message = "If your email is registered, you will receive a password reset link.",
            Token = token // In a real app, don't return this to the client
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid reset password data",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Password reset failed"
            });
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Password reset failed",
                Errors = result.Errors.Select(e => e.Description).ToList()
            });
        }

        return Ok(new AuthResponseDto
        {
            IsSuccess = true,
            Message = "Password has been reset successfully"
        });
    }

    private static UserInfoDto MapToUserInfoDto(ApplicationUser user, IList<string> roles)
    {
        return new UserInfoDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            ProfilePicture = user.ProfilePicture,
            Roles = roles.ToList(),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive
        };
    }
}
