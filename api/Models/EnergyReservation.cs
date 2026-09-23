// File: EnergyReservation.cs
// Purpose: one power trading reservation, matches the Energy Reservation collection
// Author: Sathush Nanayakkara

using MongoDB.Bson.Serialization.Attributes;

namespace SolarMicrogrid.Api.Models;

public class EnergyReservation
{
    // a guid made before insert, same idea as UserDetail
    [BsonId]
    public string Id { get; set; } = "";

    public string Nic { get; set; } = "";

    public string StationId { get; set; } = "";

    public string SlotId { get; set; } = "";

    public DateTime ScheduledTime { get; set; }

    public string State { get; set; } = "pending";

    // only set once a reservation is approved
    [BsonIgnoreIfNull]
    public string? QrData { get; set; }
}
