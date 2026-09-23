// File: ReservationTimeRequest.cs
// Purpose: the field sent when modifying a reservation, only the time can change
// Author: Sathush Nanayakkara

namespace SolarMicrogrid.Api.Models;

public class ReservationTimeRequest
{
    public DateTime ScheduledTime { get; set; }
}
