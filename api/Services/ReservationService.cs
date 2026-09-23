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

    // gets the reservation and user collections
    public ReservationService(IMongoDatabase db)
    {
        reservations = db.GetCollection<EnergyReservation>("EnergyReservation");
        users = db.GetCollection<UserDetail>("UserDetail");
    }

    // creates a reservation, must be scheduled within 7 days
    public async Task<(string? Error, EnergyReservation? Reservation)> Create(ReservationRequest request, string callerId, string callerRole)
    {
        var nic = request.Nic;
        if (callerRole == "Prosumer")
        {
            var caller = await users.Find(u => u.Id == callerId).FirstOrDefaultAsync();
            nic = caller?.Nic;
        }

        if (string.IsNullOrEmpty(nic)) return ("Prosumer nic is required", null);

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
            if (caller == null || reservation.Nic != caller.Nic) return (403, "Not your reservation", null);
        }

        return (200, null, reservation);
    }

    // updates the scheduled time of a reservation, needs 12 hours notice and the new time must be within 7 days
    public async Task<(int Status, string? Error, EnergyReservation? Reservation)> Update(string id, ReservationTimeRequest request, string callerId, string callerRole)
    {
        var (status, error, reservation) = await GetById(id, callerId, callerRole);
        if (error != null) return (status, error, null);

        if (reservation!.ScheduledTime - DateTime.UtcNow < TimeSpan.FromHours(12))
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
}
