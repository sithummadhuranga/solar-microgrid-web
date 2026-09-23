// File: UsersController.cs
// Purpose: endpoints for backoffice to manage backoffice and grid operator users
// Author: H.M.T.S.M.Dissanayake

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Backoffice")]
public class UsersController : ControllerBase
{
    private readonly UserService service;

    // gets the user service
    public UsersController(UserService service)
    {
        this.service = service;
    }

    // lists backoffice and grid operator users
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await service.GetAll();
        return Ok(users.Select(ToView));
    }

    // adds a backoffice or grid operator user
    [HttpPost]
    public async Task<IActionResult> Create(UserRequest request)
    {
        var (status, error, created) = await service.Create(request);
        if (error != null) return StatusCode(status, new { message = error });

        return StatusCode(status, ToView(created!));
    }

    // changes the name, email, role and optionally the password of a staff user
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UserRequest request)
    {
        var (status, error, updated) = await service.Update(id, request);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok(ToView(updated!));
    }

    // shapes a user for the response, the password hash never leaves the api
    private static object ToView(UserDetail user)
    {
        return new
        {
            id = user.Id,
            email = user.Email,
            fullName = user.FullName,
            role = user.Role,
            status = user.Status
        };
    }
}
