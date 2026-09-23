// File: StationService.cs
// Purpose: rules for creating, updating, deactivating, activating and deleting microgrid nodes, and the sample nodes
// Author: Christine Lowe

using MongoDB.Driver;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Services;

public class StationService
{
    private readonly IMongoCollection<SolarStation> stations;
    private readonly IMongoCollection<EnergyBookingSlot> slots;
    private readonly IMongoCollection<EnergyReservation> reservations;

    // gets the station, slot and reservation collections
    public StationService(IMongoDatabase db)
    {
        stations = db.GetCollection<SolarStation>("SolarStationInfo");
        slots = db.GetCollection<EnergyBookingSlot>("EnergyBookingSlots");
        reservations = db.GetCollection<EnergyReservation>("EnergyReservation");
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

        if (await reservations.Find(r => r.StationId == id).AnyAsync())
            return (400, "Station has reservations, deactivate it instead");

        await stations.DeleteOneAsync(s => s.Id == id);
        return (200, null);
    }

    // adds sample nodes with a few slots each, only when there are no nodes yet
    public async Task AddSampleData()
    {
        if (await stations.Find(_ => true).AnyAsync()) return;

        var samples = new List<SolarStation>
        {
            new() { Name = "Colombo Fort Hub", Address = "Fort, Colombo 01", Latitude = 6.9344, Longitude = 79.8428, CapacityKwh = 120, BatterySlotCount = 10, OpeningTime = "06:00", ClosingTime = "20:00" },
            new() { Name = "Malabe Solar Node", Address = "New Kandy Road, Malabe", Latitude = 6.9147, Longitude = 79.9729, CapacityKwh = 80, BatterySlotCount = 6, OpeningTime = "07:00", ClosingTime = "19:00" },
            new() { Name = "Kandy Lake Node", Address = "Dalada Veediya, Kandy", Latitude = 7.2936, Longitude = 80.6413, CapacityKwh = 60, BatterySlotCount = 5, OpeningTime = "06:30", ClosingTime = "18:30" },
            new() { Name = "Galle Fort Node", Address = "Church Street, Galle", Latitude = 6.0269, Longitude = 80.2170, CapacityKwh = 50, BatterySlotCount = 4, OpeningTime = "08:00", ClosingTime = "18:00" }
        };

        // slots start tomorrow so they can be booked within the 7 day limit
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);

        foreach (var station in samples)
        {
            station.Id = Guid.NewGuid().ToString();
            station.Status = "active";

            var slotList = new List<EnergyBookingSlot>();
            for (var day = 0; day < 3; day++)
            {
                foreach (var hour in new[] { 3, 7 })
                {
                    slotList.Add(new EnergyBookingSlot
                    {
                        Id = Guid.NewGuid().ToString(),
                        StationId = station.Id,
                        StartTime = tomorrow.AddDays(day).AddHours(hour),
                        EndTime = tomorrow.AddDays(day).AddHours(hour + 3),
                        TotalSlots = station.BatterySlotCount,
                        AvailableSlots = station.BatterySlotCount
                    });
                }
            }

            await stations.InsertOneAsync(station);
            await slots.InsertManyAsync(slotList);
        }
    }

    // a reservation is active while it is pending or approved
    private async Task<bool> HasActiveReservations(string stationId)
    {
        return await reservations
            .Find(r => r.StationId == stationId && (r.State == "pending" || r.State == "approved"))
            .AnyAsync();
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
