using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalaryManagementAPI.Data;
using SalaryManagementAPI.DTOs;
using SalaryManagementAPI.Models;
using SalaryManagementAPI.Services;

namespace SalaryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly JsonDataStore _store;
        private readonly ITokenService _tokenService;

        public AuthController(JsonDataStore store, ITokenService tokenService)
        {
            _store = store;
            _tokenService = tokenService;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("register")]
        public ActionResult<AuthResponseDto> Register(RegisterDto dto)
        {
            var emailExists = _store.Read(d => d.Users.Any(u => u.Email.ToLower() == dto.Email.ToLower()));
            if (emailExists)
                return Conflict(new { message = "An account with this email already exists." });

            var user = _store.Mutate(d =>
            {
                var newUser = new User
                {
                    Id = d.NextUserId++,
                    FullName = dto.FullName,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Currency = "USD"
                };
                d.Users.Add(newUser);
                return newUser;
            });

            var token = _tokenService.CreateToken(user);
            return Ok(new AuthResponseDto
            {
                Token = token,
                FullName = user.FullName,
                Email = user.Email,
                Currency = user.Currency
            });
        }

        [HttpPost("login")]
        public ActionResult<AuthResponseDto> Login(LoginDto dto)
        {
            var user = _store.Read(d => d.Users.FirstOrDefault(u => u.Email.ToLower() == dto.Email.ToLower()));
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password." });

            var token = _tokenService.CreateToken(user);
            return Ok(new AuthResponseDto
            {
                Token = token,
                FullName = user.FullName,
                Email = user.Email,
                Currency = user.Currency
            });
        }

        [Authorize]
        [HttpGet("me")]
        public ActionResult<AuthResponseDto> Me()
        {
            var user = _store.Read(d => d.Users.FirstOrDefault(u => u.Id == CurrentUserId));
            if (user == null) return NotFound();

            return Ok(new AuthResponseDto
            {
                Token = string.Empty,
                FullName = user.FullName,
                Email = user.Email,
                Currency = user.Currency
            });
        }

        [Authorize]
        [HttpPut("profile")]
        public IActionResult UpdateProfile(UpdateProfileDto dto)
        {
            var found = _store.Mutate(d =>
            {
                var user = d.Users.FirstOrDefault(u => u.Id == CurrentUserId);
                if (user == null) return false;
                user.FullName = dto.FullName;
                user.Currency = dto.Currency;
                return true;
            });

            if (!found) return NotFound();
            return Ok(new { message = "Profile updated." });
        }

        [Authorize]
        [HttpPut("change-password")]
        public IActionResult ChangePassword(ChangePasswordDto dto)
        {
            string? error = null;
            var found = _store.Mutate(d =>
            {
                var user = d.Users.FirstOrDefault(u => u.Id == CurrentUserId);
                if (user == null) return false;

                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                {
                    error = "Current password is incorrect.";
                    return true;
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                return true;
            });

            if (!found) return NotFound();
            if (error != null) return BadRequest(new { message = error });
            return Ok(new { message = "Password changed." });
        }
    }
}
