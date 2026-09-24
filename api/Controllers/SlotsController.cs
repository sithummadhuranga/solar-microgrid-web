// File: SlotsController.cs
// Purpose: endpoints for the booking slots of a station
// Author: Christine Lowe

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

[ApiController]
[Route("api/stations/{stationId}/slots")]
[Authorize]
public class SlotsController : ControllerBase
{
    private readonly SlotService service;

    // gets the slot service
    public SlotsController(SlotService service)
    {
        this.service = service;
    }

    // lists the slots of a station, ?upcoming=true skips slots that have ended
    [HttpGet]
    public async Task<IActionResult> GetAll(string stationId, [FromQuery] bool upcoming = false)
    {
        var (status, error, slots) = await service.GetForStation(stationId, User.IsInRole("Prosumer"), upcoming);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok(slots);
    }

    // adds a slot to a station
    [HttpPost]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Create(string stationId, EnergyBookingSlot slot)
    {
        var (status, error, created) = await service.Create(stationId, slot);
        if (error != null) return StatusCode(status, new { message = error });

        return StatusCode(status, created);
    }

    // changes the times and size of a slot
    [HttpPut("{id}")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Update(string stationId, string id, EnergyBookingSlot slot)
    {
        var (status, error, updated) = await service.Update(stationId, id, slot);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok(updated);
    }

    // sets how many battery slots are still free, grid operators can do this too
    [HttpPut("{id}/availability")]
    [Authorize(Roles = "Backoffice,GridOperator")]
    public async Task<IActionResult> UpdateAvailability(string stationId, string id, SlotAvailabilityRequest request)
    {
        var (status, error, updated) = await service.UpdateAvailability(stationId, id, request.AvailableSlots);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok(updated);
    }

    // deletes a slot
    [HttpDelete("{id}")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Delete(string stationId, string id)
    {
        var (status, error) = await service.Delete(stationId, id);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok();
    }
}
