// File: SolarStation.cs
// Purpose: one microgrid node, saved in the SolarStationInfo collection

using MongoDB.Bson.Serialization.Attributes;

namespace SolarMicrogrid.Api.Models;

public class SolarStation
{
    // a guid made before insert, same idea as user ids
    [BsonId]
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Address { get; set; } = "";

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double CapacityKwh { get; set; }

    // how many battery storage slots the node has in total
    public int BatterySlotCount { get; set; }

    // daily schedule of the node, kept as "HH:mm"
    public string OpeningTime { get; set; } = "";

    public string ClosingTime { get; set; } = "";

    public string Status { get; set; } = "active";
}
