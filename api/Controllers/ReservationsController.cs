// File: ReservationsController.cs
// Purpose: endpoints for creating, updating and cancelling reservations
// Author: Sathush Nanayakkara

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly ReservationService service;

    // gets the reservation service
    public ReservationsController(ReservationService service)
    {
        this.service = service;
    }

    // creates a reservation for the logged in prosumer, or for a nic given by staff
    [HttpPost]
    [Authorize(Roles = "Prosumer,Backoffice,GridOperator")]
    public async Task<IActionResult> Create(ReservationRequest request)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var callerRole = User.FindFirstValue(ClaimTypes.Role)!;

        var (error, reservation) = await service.Create(request, callerId, callerRole);
        if (error != null) return BadRequest(new { message = error });

        return Ok(reservation);
    }

    // gets one reservation, a prosumer can only see their own
    [HttpGet("{id}")]
    [Authorize(Roles = "Prosumer,Backoffice,GridOperator")]
    public async Task<IActionResult> GetById(string id)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var callerRole = User.FindFirstValue(ClaimTypes.Role)!;

        var (status, error, reservation) = await service.GetById(id, callerId, callerRole);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok(reservation);
    }

    // updates the scheduled time of a reservation, needs 12 hours notice
    [HttpPut("{id}")]
    [Authorize(Roles = "Prosumer,Backoffice,GridOperator")]
    public async Task<IActionResult> Update(string id, ReservationTimeRequest request)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var callerRole = User.FindFirstValue(ClaimTypes.Role)!;

        var (status, error, reservation) = await service.Update(id, request, callerId, callerRole);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok(reservation);
    }

    // cancels a reservation, needs 12 hours notice
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Prosumer,Backoffice,GridOperator")]
    public async Task<IActionResult> Cancel(string id)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var callerRole = User.FindFirstValue(ClaimTypes.Role)!;

        var (status, error, reservation) = await service.Cancel(id, callerId, callerRole);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok(reservation);
    }
}
