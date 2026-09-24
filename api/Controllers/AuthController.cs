// File: AuthController.cs
// Purpose: the login endpoint
// Author: H.M.T.S.M.Dissanayake

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly AuthService service;

    // gets the auth service
    public AuthController(AuthService service)
    {
        this.service = service;
    }

    // logs a user in with their nic or email and password
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var (status, error, token, user) = await service.Login(request);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok(new
        {
            token,
            id = user!.Id,
            fullName = user.FullName,
            role = user.Role
        });
    }

    // returns the real role and status for the logged in token, the page shell uses this to check
    // a role a client only stored locally, never trust that copy on its own
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await service.GetById(id);
        if (user == null) return NotFound();

        return Ok(new
        {
            id = user.Id,
            fullName = user.FullName,
            role = user.Role,
            status = user.Status
        });
    }
}
