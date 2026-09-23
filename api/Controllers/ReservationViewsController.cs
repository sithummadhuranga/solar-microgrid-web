// File: ReservationViewsController.cs
// Purpose: booking lists and counts for a prosumer, and the grid operator qr endpoints
// Author: T.H.Nimnath Nadushka

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationViewsController : ControllerBase
{
    private readonly ReservationViewService service;

    // gets the reservation view service
    public ReservationViewsController(ReservationViewService service)
    {
        this.service = service;
    }

    // lists the logged in prosumer's own reservations, state is a comma separated list
    [HttpGet("mine")]
    [Authorize(Roles = "Prosumer")]
    public async Task<IActionResult> Mine(string state = "", string search = "")
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await service.ListMine(callerId, state, search));
    }

    // gives the dashboard its pending count and its approved future count
    [HttpGet("summary")]
    [Authorize(Roles = "Prosumer")]
    public async Task<IActionResult> Summary()
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await service.Summary(callerId));
    }

    // checks a scanned qr code against the stored reservation before the transfer is finalised
    [HttpPost("verify")]
    [Authorize(Roles = "GridOperator")]
    public async Task<IActionResult> Verify(QrVerifyRequest request)
    {
        var (status, error, reservation) = await service.Verify(request.QrData);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok(reservation);
    }

    // marks the energy transfer done after the operator has verified the code
    [HttpPost("{id}/complete")]
    [Authorize(Roles = "GridOperator")]
    public async Task<IActionResult> Complete(string id)
    {
        var (status, error, reservation) = await service.Complete(id);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok(reservation);
    }
}
