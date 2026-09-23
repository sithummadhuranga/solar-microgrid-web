// File: SlotService.cs
// Purpose: rules for the booking slots of a station and their availability
// Author: Christine Lowe

using MongoDB.Driver;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Services;

public class SlotService
{
    private readonly IMongoCollection<EnergyBookingSlot> slots;
    private readonly IMongoCollection<SolarStation> stations;
    private readonly IMongoCollection<EnergyReservation> reservations;

    // gets the slot, station and reservation collections
    public SlotService(IMongoDatabase db)
    {
        slots = db.GetCollection<EnergyBookingSlot>("EnergyBookingSlots");
        stations = db.GetCollection<SolarStation>("SolarStationInfo");
        reservations = db.GetCollection<EnergyReservation>("EnergyReservation");
    }

    // gets the slots of a station in time order, prosumers only get slots that have not ended on active stations
    public async Task<(int Status, string? Error, List<EnergyBookingSlot>? Slots)> GetForStation(string stationId, bool isProsumer)
    {
        var station = await stations.Find(s => s.Id == stationId).FirstOrDefaultAsync();
        if (station == null || (isProsumer && station.Status != "active")) return (404, "Station not found", null);

        var now = DateTime.UtcNow;
        var list = isProsumer
            ? await slots.Find(s => s.StationId == stationId && s.EndTime > now).SortBy(s => s.StartTime).ToListAsync()
            : await slots.Find(s => s.StationId == stationId).SortBy(s => s.StartTime).ToListAsync();
        return (200, null, list);
    }

    // adds a slot to an active station, every battery slot in it starts free
    public async Task<(int Status, string? Error, EnergyBookingSlot? Slot)> Create(string stationId, EnergyBookingSlot slot)
    {
        var station = await stations.Find(s => s.Id == stationId).FirstOrDefaultAsync();
        if (station == null) return (404, "Station not found", null);
        if (station.Status != "active") return (400, "Slots can only be added to an active station", null);

        var error = Check(slot, station);
        if (error != null) return (400, error, null);
        if (slot.StartTime <= DateTime.UtcNow) return (400, "Start time must be in the future", null);

        slot.Id = Guid.NewGuid().ToString();
        slot.StationId = stationId;
        slot.AvailableSlots = slot.TotalSlots;
        await slots.InsertOneAsync(slot);
        return (201, null, slot);
    }

    // changes the times and size of a slot, times are locked while it has active reservations
    public async Task<(int Status, string? Error, EnergyBookingSlot? Slot)> Update(string stationId, string id, EnergyBookingSlot changes)
    {
        var slot = await slots.Find(s => s.Id == id && s.StationId == stationId).FirstOrDefaultAsync();
        if (slot == null) return (404, "Slot not found", null);

        var station = await stations.Find(s => s.Id == stationId).FirstOrDefaultAsync();
        var error = Check(changes, station!);
        if (error != null) return (400, error, null);

        var timesChanged = changes.StartTime != slot.StartTime || changes.EndTime != slot.EndTime;
        if (timesChanged && changes.StartTime <= DateTime.UtcNow)
            return (400, "Start time must be in the future", null);
        if (timesChanged && await HasActiveReservations(id))
            return (400, "Slot has active reservations, its times cannot be changed", null);

        slot.StartTime = changes.StartTime;
        slot.EndTime = changes.EndTime;
        slot.TotalSlots = changes.TotalSlots;
        slot.AvailableSlots = Math.Min(slot.AvailableSlots, slot.TotalSlots);
        await slots.ReplaceOneAsync(s => s.Id == id, slot);
        return (200, null, slot);
    }

    // sets how many battery slots are still free in a slot
    public async Task<(int Status, string? Error, EnergyBookingSlot? Slot)> UpdateAvailability(string stationId, string id, int availableSlots)
    {
        var slot = await slots.Find(s => s.Id == id && s.StationId == stationId).FirstOrDefaultAsync();
        if (slot == null) return (404, "Slot not found", null);

        if (availableSlots < 0 || availableSlots > slot.TotalSlots)
            return (400, $"Available slots must be between 0 and {slot.TotalSlots}", null);

        slot.AvailableSlots = availableSlots;
        await slots.UpdateOneAsync(s => s.Id == id, Builders<EnergyBookingSlot>.Update.Set(s => s.AvailableSlots, availableSlots));
        return (200, null, slot);
    }

    // deletes a slot, blocked while it has active reservations
    public async Task<(int Status, string? Error)> Delete(string stationId, string id)
    {
        var slot = await slots.Find(s => s.Id == id && s.StationId == stationId).FirstOrDefaultAsync();
        if (slot == null) return (404, "Slot not found");

        if (await HasActiveReservations(id))
            return (400, "Slot has active reservations and cannot be deleted");

        await slots.DeleteOneAsync(s => s.Id == id);
        return (200, null);
    }

    // a reservation is active while it is pending or approved
    private async Task<bool> HasActiveReservations(string slotId)
    {
        return await reservations
            .Find(r => r.SlotId == slotId && (r.State == "pending" || r.State == "approved"))
            .AnyAsync();
    }

    // checks the slot fields against its station, returns an error message or null when they are fine
    private static string? Check(EnergyBookingSlot slot, SolarStation station)
    {
        if (slot.EndTime <= slot.StartTime) return "End time must be after start time";
        if (slot.TotalSlots < 1) return "Battery storage slots must be at least 1";
        if (slot.TotalSlots > station.BatterySlotCount)
            return $"This station only has {station.BatterySlotCount} battery storage slots";

        return null;
    }
}
