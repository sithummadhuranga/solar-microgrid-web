// File: ProsumersController.cs
// Purpose: endpoints for registering and managing prosumers
// Author: H.M.T.S.M.Dissanayake

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

[ApiController]
[Route("api/prosumers")]
[Authorize]
public class ProsumersController : ControllerBase
{
    private readonly ProsumerService service;

    // gets the prosumer service
    public ProsumersController(ProsumerService service)
    {
        this.service = service;
    }

    // registers a new prosumer, used by backoffice and later the mobile app
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(ProsumerRegisterRequest request)
    {
        var (status, error, prosumer) = await service.Register(request);
        if (error != null) return StatusCode(status, new { message = error });

        return StatusCode(status, ToView(prosumer!));
    }

    // lists every prosumer
    [HttpGet]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> GetAll()
    {
        var prosumers = await service.GetAll();
        return Ok(prosumers.Select(ToView));
    }

    // lists prosumers waiting for activation
    [HttpGet("pending")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> GetPending()
    {
        var prosumers = await service.GetPending();
        return Ok(prosumers.Select(ToView));
    }

    // looks a prosumer up by nic, a query on the unique nic field
    [HttpGet("by-nic/{nic}")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> GetByNic(string nic)
    {
        var prosumer = await service.GetByNic(nic);
        if (prosumer == null) return NotFound(new { message = "Prosumer not found" });

        return Ok(ToView(prosumer));
    }

    // reads the logged in prosumer's own profile, the id comes from the token
    [HttpGet("me")]
    [Authorize(Roles = "Prosumer")]
    public async Task<IActionResult> GetMe()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var prosumer = await service.Get(id);
        if (prosumer == null) return NotFound(new { message = "Prosumer not found" });

        return Ok(ToView(prosumer));
    }

    // updates the logged in prosumer's own profile, the id comes from the token, the nic cannot change here
    [HttpPut("me")]
    [Authorize(Roles = "Prosumer")]
    public async Task<IActionResult> UpdateMe(ProsumerProfileRequest request)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (status, error, prosumer) = await service.UpdateOwn(id, request);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok(ToView(prosumer!));
    }

    // the logged in prosumer asks to be deactivated, backoffice still has to act on it
    [HttpPost("me/deactivate-request")]
    [Authorize(Roles = "Prosumer")]
    public async Task<IActionResult> RequestOwnDeactivation()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (status, error) = await service.RequestDeactivation(id);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok();
    }

    // gets one prosumer by id
    [HttpGet("{id}")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Get(string id)
    {
        var prosumer = await service.Get(id);
        if (prosumer == null) return NotFound(new { message = "Prosumer not found" });

        return Ok(ToView(prosumer));
    }

    // backoffice updates any prosumer's profile by id, including the nic
    [HttpPut("{id}")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Update(string id, ProsumerUpdateRequest request)
    {
        var (status, error, prosumer) = await service.UpdateByBackoffice(id, request);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok(ToView(prosumer!));
    }

    // activates a pending prosumer
    [HttpPost("{id}/activate")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Activate(string id)
    {
        var (status, error) = await service.Activate(id);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok();
    }

    // deactivates an active prosumer
    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Deactivate(string id)
    {
        var (status, error) = await service.Deactivate(id);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok();
    }

    // reactivates a deactivated prosumer, only backoffice can do this
    [HttpPost("{id}/reactivate")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Reactivate(string id)
    {
        var (status, error) = await service.Reactivate(id);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok();
    }

    // shapes a prosumer for the response, the password hash never leaves the api
    private static object ToView(UserDetail prosumer)
    {
        return new
        {
            id = prosumer.Id,
            nic = prosumer.Nic,
            fullName = prosumer.FullName,
            phone = prosumer.Phone,
            address = prosumer.Address,
            status = prosumer.Status,
            deactivationRequested = prosumer.DeactivationRequested
        };
    }
}
