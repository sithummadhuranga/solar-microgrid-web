// File: ReservationService.cs
// Purpose: rules for creating, updating and cancelling reservations
// Author: Sathush Nanayakkara

using MongoDB.Driver;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Services;

public class ReservationService
{
    private readonly IMongoCollection<EnergyReservation> reservations;
    private readonly IMongoCollection<UserDetail> users;
    private readonly IMongoCollection<SolarStation> stations;
    private readonly IMongoCollection<EnergyBookingSlot> slots;

    // gets the reservation, user, station and slot collections
    public ReservationService(IMongoDatabase db)
    {
        reservations = db.GetCollection<EnergyReservation>("EnergyReservation");
        users = db.GetCollection<UserDetail>("UserDetail");
        stations = db.GetCollection<SolarStation>("SolarStationInfo");
        slots = db.GetCollection<EnergyBookingSlot>("EnergyBookingSlots");
    }

    // creates a reservation for an active node and one of its slots, must be scheduled within 7 days
    public async Task<(string? Error, EnergyReservation? Reservation)> Create(ReservationRequest request, string callerId, string callerRole)
    {
        var nic = request.Nic;
        if (callerRole == "Prosumer")
        {
            var caller = await users.Find(u => u.Id == callerId).FirstOrDefaultAsync();
            nic = caller?.Nic;
        }

        if (string.IsNullOrWhiteSpace(nic)) return ("Prosumer nic is required", null);
        if (string.IsNullOrWhiteSpace(request.StationId)) return ("Microgrid node is required", null);
        if (string.IsNullOrWhiteSpace(request.SlotId)) return ("Slot is required", null);

        // staff book for a nic they type, so it must belong to a real prosumer
        if (callerRole != "Prosumer")
        {
            var prosumer = await users.Find(u => u.Role == "Prosumer" && u.Nic == nic).FirstOrDefaultAsync();
            if (prosumer == null) return ("Prosumer not found", null);
        }

        var station = await stations.Find(s => s.Id == request.StationId).FirstOrDefaultAsync();
        if (station == null) return ("Station not found", null);
        if (station.Status != "active") return ("This microgrid node is not active", null);

        // the slot must belong to the chosen node
        var slot = await slots.Find(s => s.Id == request.SlotId && s.StationId == request.StationId).FirstOrDefaultAsync();
        if (slot == null) return ("Slot not found", null);

        if (request.ScheduledTime < DateTime.UtcNow || request.ScheduledTime > DateTime.UtcNow.AddDays(7))
            return ("Reservations must be scheduled within 7 days", null);

        var reservation = new EnergyReservation
        {
            Id = Guid.NewGuid().ToString(),
            Nic = nic,
            StationId = request.StationId,
            SlotId = request.SlotId,
            ScheduledTime = request.ScheduledTime,
            State = "pending"
        };

        await reservations.InsertOneAsync(reservation);
        return (null, reservation);
    }

    // finds a reservation by id, a prosumer can only see their own
    public async Task<(int Status, string? Error, EnergyReservation? Reservation)> GetById(string id, string callerId, string callerRole)
    {
        var reservation = await reservations.Find(r => r.Id == id).FirstOrDefaultAsync();
        if (reservation == null) return (404, "Reservation not found", null);

        if (callerRole == "Prosumer")
        {
            var caller = await users.Find(u => u.Id == callerId).FirstOrDefaultAsync();
            if (caller == null || reservation.Nic != caller.Nic) return (404, "Reservation not found", null);
        }

        return (200, null, reservation);
    }

    // updates the scheduled time of a pending or approved reservation, needs 12 hours notice and the new time must be within 7 days
    public async Task<(int Status, string? Error, EnergyReservation? Reservation)> Update(string id, ReservationTimeRequest request, string callerId, string callerRole)
    {
        var (status, error, reservation) = await GetById(id, callerId, callerRole);
        if (error != null) return (status, error, null);

        if (reservation!.State != "pending" && reservation.State != "approved")
            return (400, "Only a pending or approved reservation can be changed", null);

        if (reservation.ScheduledTime - DateTime.UtcNow < TimeSpan.FromHours(12))
            return (400, "Updating a reservation needs at least 12 hours notice", null);

        if (request.ScheduledTime < DateTime.UtcNow || request.ScheduledTime > DateTime.UtcNow.AddDays(7))
            return (400, "Reservations must be scheduled within 7 days", null);

        reservation.ScheduledTime = request.ScheduledTime;

        await reservations.ReplaceOneAsync(r => r.Id == id, reservation);
        return (200, null, reservation);
    }

    // cancels a reservation, needs 12 hours notice, only a pending or approved one can be cancelled
    public async Task<(int Status, string? Error, EnergyReservation? Reservation)> Cancel(string id, string callerId, string callerRole)
    {
        var (status, error, reservation) = await GetById(id, callerId, callerRole);
        if (error != null) return (status, error, null);

        if (reservation!.State != "pending" && reservation.State != "approved")
            return (400, "Only a pending or approved reservation can be cancelled", null);

        if (reservation.ScheduledTime - DateTime.UtcNow < TimeSpan.FromHours(12))
            return (400, "Cancelling a reservation needs at least 12 hours notice", null);

        reservation.State = "cancelled";
        await reservations.ReplaceOneAsync(r => r.Id == id, reservation);
        return (200, null, reservation);
    }

    // approves a pending reservation that is still to come and generates its qr code, BR-7
    public async Task<(int Status, string? Error, EnergyReservation? Reservation)> Approve(string id)
    {
        var reservation = await reservations.Find(r => r.Id == id).FirstOrDefaultAsync();
        if (reservation == null) return (404, "Reservation not found", null);

        if (reservation.State != "pending")
            return (400, "Only a pending reservation can be approved", null);

        if (reservation.ScheduledTime < DateTime.UtcNow)
            return (400, "This reservation time has already passed", null);

        reservation.State = "approved";
        reservation.QrData = Guid.NewGuid().ToString();

        await reservations.ReplaceOneAsync(r => r.Id == id, reservation);
        return (200, null, reservation);
    }
}
