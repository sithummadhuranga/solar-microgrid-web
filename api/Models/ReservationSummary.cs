// File: ReservationSummary.cs
// Purpose: the two counts the prosumer dashboard shows
// Author: T.H.Nimnath Nadushka

namespace SolarMicrogrid.Api.Models;

public class ReservationSummary
{
    public int Pending { get; set; }

    // approved reservations whose scheduled time has not passed yet
    public int ApprovedFuture { get; set; }
}
