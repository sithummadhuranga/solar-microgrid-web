// File: EnergyBookingSlot.cs
// Purpose: one time window at a station that prosumers can book, saved in the EnergyBookingSlots collection
// Author: Christine Lowe

using MongoDB.Bson.Serialization.Attributes;

namespace SolarMicrogrid.Api.Models;

public class EnergyBookingSlot
{
    // a guid made before insert
    [BsonId]
    public string Id { get; set; } = "";

    public string StationId { get; set; } = "";

    // start and end are stored in utc
    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    // battery storage slots offered in this window, never more than the station has
    public int TotalSlots { get; set; }

    // battery storage slots still free, grid operators can change this
    public int AvailableSlots { get; set; }
}
