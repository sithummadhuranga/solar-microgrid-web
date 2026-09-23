// File: StationService.cs
// Purpose: rules for creating, updating, deactivating, activating and deleting microgrid nodes
// Author: Christine Lowe

using MongoDB.Bson;
using MongoDB.Driver;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Services;

public class StationService
{
    private readonly IMongoCollection<SolarStation> stations;
    private readonly IMongoCollection<EnergyBookingSlot> slots;
    private readonly IMongoCollection<BsonDocument> reservations;

    // gets the station, slot and reservation collections
    public StationService(IMongoDatabase db)
    {
        stations = db.GetCollection<SolarStation>("SolarStationInfo");
        slots = db.GetCollection<EnergyBookingSlot>("EnergyBookingSlots");
        reservations = db.GetCollection<BsonDocument>("EnergyReservation");
    }

    // gets all stations, or only the active ones
    public async Task<List<SolarStation>> GetAll(bool activeOnly)
    {
        if (activeOnly)
            return await stations.Find(s => s.Status == "active").SortBy(s => s.Name).ToListAsync();

        return await stations.Find(_ => true).SortBy(s => s.Name).ToListAsync();
    }

    // gets one station by id, null if it does not exist
    public async Task<SolarStation?> Get(string id)
    {
        return await stations.Find(s => s.Id == id).FirstOrDefaultAsync();
    }

    // adds a new station, it starts as active
    public async Task<(int Status, string? Error, SolarStation? Station)> Create(SolarStation station)
    {
        var error = Check(station);
        if (error != null) return (400, error, null);

        station.Id = Guid.NewGuid().ToString();
        station.Status = "active";
        await stations.InsertOneAsync(station);
        return (201, null, station);
    }

    // changes the details and schedule of a station, the status is left as it is
    public async Task<(int Status, string? Error, SolarStation? Station)> Update(string id, SolarStation changes)
    {
        var station = await Get(id);
        if (station == null) return (404, "Station not found", null);

        var error = Check(changes);
        if (error != null) return (400, error, null);

        // a slot cannot offer more battery storage slots than its station has
        if (await slots.Find(s => s.StationId == id && s.TotalSlots > changes.BatterySlotCount).AnyAsync())
            return (400, "Some slots use more battery storage slots than that, change those slots first", null);

        changes.Id = station.Id;
        changes.Status = station.Status;
        await stations.ReplaceOneAsync(s => s.Id == id, changes);
        return (200, null, changes);
    }

    // deactivates a station, blocked while it has active reservations
    public async Task<(int Status, string? Error)> Deactivate(string id)
    {
        var station = await Get(id);
        if (station == null) return (404, "Station not found");
        if (station.Status == "deactivated") return (400, "Station is already deactivated");

        if (await HasActiveReservations(id))
            return (400, "Station has active reservations and cannot be deactivated");

        await stations.UpdateOneAsync(s => s.Id == id, Builders<SolarStation>.Update.Set(s => s.Status, "deactivated"));
        return (200, null);
    }

    // makes a deactivated station active again
    public async Task<(int Status, string? Error)> Activate(string id)
    {
        var station = await Get(id);
        if (station == null) return (404, "Station not found");
        if (station.Status == "active") return (400, "Station is already active");

        await stations.UpdateOneAsync(s => s.Id == id, Builders<SolarStation>.Update.Set(s => s.Status, "active"));
        return (200, null);
    }

    // deletes a station, only when it has no slots and no reservations at all
    public async Task<(int Status, string? Error)> Delete(string id)
    {
        var station = await Get(id);
        if (station == null) return (404, "Station not found");

        if (await slots.Find(s => s.StationId == id).AnyAsync())
            return (400, "Station has slots, delete them first or deactivate the station");

        if (await reservations.Find(Builders<BsonDocument>.Filter.Eq("StationId", id)).AnyAsync())
            return (400, "Station has reservations, deactivate it instead");

        await stations.DeleteOneAsync(s => s.Id == id);
        return (200, null);
    }

    // a reservation is active while it is pending or approved
    private async Task<bool> HasActiveReservations(string stationId)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("StationId", stationId)
            & Builders<BsonDocument>.Filter.In("State", new[] { "pending", "approved" });
        return await reservations.Find(filter).AnyAsync();
    }

    // checks the station fields, returns an error message or null when they are fine
    private static string? Check(SolarStation station)
    {
        if (string.IsNullOrWhiteSpace(station.Name)) return "Name is required";
        if (string.IsNullOrWhiteSpace(station.Address)) return "Address is required";
        if (station.Latitude < -90 || station.Latitude > 90) return "Latitude must be between -90 and 90";
        if (station.Longitude < -180 || station.Longitude > 180) return "Longitude must be between -180 and 180";
        if (station.CapacityKwh <= 0) return "Capacity must be more than 0";
        if (station.BatterySlotCount < 1) return "Battery storage slots must be at least 1";

        if (!TimeOnly.TryParseExact(station.OpeningTime, "HH:mm", out var opening))
            return "Opening time must look like 06:00";
        if (!TimeOnly.TryParseExact(station.ClosingTime, "HH:mm", out var closing))
            return "Closing time must look like 18:00";
        if (opening >= closing) return "Opening time must be before closing time";

        return null;
    }
}
