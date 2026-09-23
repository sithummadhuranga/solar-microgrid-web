// File: StationsController.cs
// Purpose: endpoints for microgrid nodes
// Author: Christine Lowe

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarMicrogrid.Api.Models;
using SolarMicrogrid.Api.Services;

namespace SolarMicrogrid.Api.Controllers;

[ApiController]
[Route("api/stations")]
[Authorize]
public class StationsController : ControllerBase
{
    private readonly StationService service;

    // gets the station service
    public StationsController(StationService service)
    {
        this.service = service;
    }

    // lists stations, prosumers only see the active ones
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var stations = await service.GetAll(User.IsInRole("Prosumer"));
        return Ok(stations);
    }

    // gets one station
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var station = await service.Get(id);
        if (station == null || (User.IsInRole("Prosumer") && station.Status != "active"))
            return NotFound(new { message = "Station not found" });

        return Ok(station);
    }

    // adds a station
    [HttpPost]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Create(SolarStation station)
    {
        var (status, error, created) = await service.Create(station);
        if (error != null) return StatusCode(status, new { message = error });

        return StatusCode(status, created);
    }

    // changes the details and schedule of a station
    [HttpPut("{id}")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Update(string id, SolarStation station)
    {
        var (status, error, updated) = await service.Update(id, station);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok(updated);
    }

    // deactivates a station
    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Deactivate(string id)
    {
        var (status, error) = await service.Deactivate(id);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok();
    }

    // activates a deactivated station
    [HttpPost("{id}/activate")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Activate(string id)
    {
        var (status, error) = await service.Activate(id);
        if (error != null) return StatusCode(status, new { message = error });

        return Ok();
    }
}
