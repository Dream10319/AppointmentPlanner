using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AppointmentPlanner.Models.Identity;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace AppointmentPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserController> _logger;

    public UserController(UserManager<ApplicationUser> userManager, ILogger<UserController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserInfoDto
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
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid profile data",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "User not found"
            });
        }

        // Update user properties
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.PhoneNumber = model.PhoneNumber;
        user.ProfilePicture = model.ProfilePicture;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Profile update failed",
                Errors = result.Errors.Select(e => e.Description).ToList()
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new AuthResponseDto
        {
            IsSuccess = true,
            Message = "Profile updated successfully",
            User = new UserInfoDto
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
            }
        });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid password data",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "User not found"
            });
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Password change failed",
                Errors = result.Errors.Select(e => e.Description).ToList()
            });
        }

        return Ok(new AuthResponseDto
        {
            IsSuccess = true,
            Message = "Password changed successfully"
        });
    }

    [HttpGet("users")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var users = _userManager.Users
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var userDtos = new List<UserInfoDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserInfoDto
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
            });
        }

        var totalUsers = _userManager.Users.Count();

        return Ok(new
        {
            Users = userDtos,
            TotalCount = totalUsers,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize)
        });
    }

    [HttpPut("users/{userId}/toggle-status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleUserStatus(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "User not found"
            });
        }

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Failed to update user status",
                Errors = result.Errors.Select(e => e.Description).ToList()
            });
        }

        return Ok(new AuthResponseDto
        {
            IsSuccess = true,
            Message = $"User {(user.IsActive ? "activated" : "deactivated")} successfully"
        });
    }
}

public class UpdateProfileDto
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? PhoneNumber { get; set; }

    [StringLength(500, ErrorMessage = "Profile picture URL cannot exceed 500 characters")]
    public string? ProfilePicture { get; set; }
}
