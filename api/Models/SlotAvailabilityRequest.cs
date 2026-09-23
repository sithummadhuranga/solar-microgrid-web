// File: SlotAvailabilityRequest.cs
// Purpose: what the slot availability endpoint reads from the request body

namespace SolarMicrogrid.Api.Models;

public class SlotAvailabilityRequest
{
    public int AvailableSlots { get; set; }
}
