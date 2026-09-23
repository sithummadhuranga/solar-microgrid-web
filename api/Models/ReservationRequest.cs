// File: ReservationRequest.cs
// Purpose: the fields sent when creating or updating a reservation
// Author: Sathush Nanayakkara

namespace SolarMicrogrid.Api.Models;

public class ReservationRequest
{
    // only sent when backoffice or a grid operator books for a prosumer, ignored for a prosumer's own booking
    public string? Nic { get; set; }

    public string StationId { get; set; } = "";

    public string SlotId { get; set; } = "";

    public DateTime ScheduledTime { get; set; }
}
