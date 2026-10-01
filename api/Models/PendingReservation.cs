// File: PendingReservation.cs
// Purpose: one pending reservation with the node name and slot times, so staff do not see ids
// Author: Sathush Nanayakkara

namespace SolarMicrogrid.Api.Models;

public class PendingReservation
{
    // needed by the approve and cancel buttons, the web page does not show it
    public string Id { get; set; } = "";

    public string Nic { get; set; } = "";

    public string StationName { get; set; } = "";

    public DateTime SlotStart { get; set; }

    public DateTime SlotEnd { get; set; }

    public DateTime ScheduledTime { get; set; }

    public string State { get; set; } = "";
}
