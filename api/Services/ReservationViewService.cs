// File: ReservationViewService.cs
// Purpose: reads a prosumer's own reservations and finishes a job after a qr scan
// Author: T.H.Nimnath Nadushka

using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using SolarMicrogrid.Api.Models;

namespace SolarMicrogrid.Api.Services;

public class ReservationViewService
{
    private readonly IMongoCollection<EnergyReservation> reservations;
    private readonly IMongoCollection<SolarStation> stations;
    private readonly IMongoCollection<UserDetail> users;
    private readonly IMongoCollection<EnergyBookingSlot> slots;

    // gets the reservation, station, user and slot collections
    public ReservationViewService(IMongoDatabase db)
    {
        reservations = db.GetCollection<EnergyReservation>("EnergyReservation");
        stations = db.GetCollection<SolarStation>("SolarStationInfo");
        users = db.GetCollection<UserDetail>("UserDetail");
        slots = db.GetCollection<EnergyBookingSlot>("EnergyBookingSlots");
    }

    // lists every pending reservation that is still to come, with node names and slot times for staff
    public async Task<List<PendingReservation>> ListPending()
    {
        var waiting = await reservations
            .Find(r => r.State == "pending" && r.ScheduledTime > DateTime.UtcNow)
            .SortBy(r => r.ScheduledTime)
            .ToListAsync();

        var stationNames = (await stations.Find(_ => true).ToListAsync()).ToDictionary(s => s.Id, s => s.Name);
        var slotList = (await slots.Find(_ => true).ToListAsync()).ToDictionary(s => s.Id);

        return waiting.Select(r =>
        {
            slotList.TryGetValue(r.SlotId, out var slot);
            return new PendingReservation
            {
                Id = r.Id,
                Nic = r.Nic,
                StationName = stationNames.GetValueOrDefault(r.StationId, "Removed node"),
                SlotStart = slot?.StartTime ?? r.ScheduledTime,
                SlotEnd = slot?.EndTime ?? r.ScheduledTime,
                ScheduledTime = r.ScheduledTime,
                State = r.State
            };
        }).ToList();
    }

    // lists the caller's own reservations, narrowed by state and by a search on the node name
    public async Task<List<EnergyReservation>> ListMine(string callerId, string state, string search)
    {
        var nic = await NicOf(callerId);
        if (nic == null) return new List<EnergyReservation>();

        var filter = Builders<EnergyReservation>.Filter.Eq(r => r.Nic, nic);

        var states = state.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (states.Length > 0)
            filter &= Builders<EnergyReservation>.Filter.In(r => r.State, states);

        if (!string.IsNullOrWhiteSpace(search))
            filter &= Builders<EnergyReservation>.Filter.In(r => r.StationId, await StationIdsNamed(search));

        return await reservations.Find(filter).SortBy(r => r.ScheduledTime).ToListAsync();
    }

    // lists every reservation for staff to monitor, narrowed by state and by a search on nic or node name
    public async Task<List<EnergyReservation>> ListAll(string state, string search)
    {
        var filter = Builders<EnergyReservation>.Filter.Empty;

        var states = state.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (states.Length > 0)
            filter &= Builders<EnergyReservation>.Filter.In(r => r.State, states);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = new BsonRegularExpression(Regex.Escape(search), "i");
            filter &= Builders<EnergyReservation>.Filter.Or(
                Builders<EnergyReservation>.Filter.Regex(r => r.Nic, pattern),
                Builders<EnergyReservation>.Filter.In(r => r.StationId, await StationIdsNamed(search)));
        }

        return await reservations.Find(filter).SortBy(r => r.ScheduledTime).ToListAsync();
    }

    // counts the caller's pending reservations and the approved ones still to come
    public async Task<ReservationSummary> Summary(string callerId)
    {
        var nic = await NicOf(callerId);
        if (nic == null) return new ReservationSummary();

        var mine = Builders<EnergyReservation>.Filter.Eq(r => r.Nic, nic);

        var pending = await reservations.CountDocumentsAsync(
            mine & Builders<EnergyReservation>.Filter.Eq(r => r.State, "pending"));

        var approvedFuture = await reservations.CountDocumentsAsync(
            mine
            & Builders<EnergyReservation>.Filter.Eq(r => r.State, "approved")
            & Builders<EnergyReservation>.Filter.Gt(r => r.ScheduledTime, DateTime.UtcNow));

        return new ReservationSummary
        {
            Pending = (int)pending,
            ApprovedFuture = (int)approvedFuture
        };
    }

    // finds the reservation behind a scanned qr code, it must still be approved
    public async Task<(int Status, string? Error, EnergyReservation? Reservation)> Verify(string qrData)
    {
        if (string.IsNullOrWhiteSpace(qrData)) return (400, "QR data is required", null);

        var reservation = await reservations.Find(r => r.QrData == qrData).FirstOrDefaultAsync();
        if (reservation == null) return (404, "No reservation matches this QR code", null);

        // the code stays readable after the job is over, so the state is what decides
        if (reservation.State != "approved")
            return (400, "This reservation is " + reservation.State + ", it cannot be finalised", null);

        return (200, null, reservation);
    }

    // marks the energy transfer done once the operator has verified the code
    public async Task<(int Status, string? Error, EnergyReservation? Reservation)> Complete(string id)
    {
        var reservation = await reservations.Find(r => r.Id == id).FirstOrDefaultAsync();
        if (reservation == null) return (404, "Reservation not found", null);

        if (reservation.State != "approved")
            return (400, "Only an approved reservation can be marked done", null);

        reservation.State = "done";
        await reservations.ReplaceOneAsync(r => r.Id == id, reservation);
        return (200, null, reservation);
    }

    // reads the nic of the logged in prosumer, the token only carries the user id
    private async Task<string?> NicOf(string callerId)
    {
        var user = await users.Find(u => u.Id == callerId).FirstOrDefaultAsync();
        return user?.Nic;
    }

    // finds the nodes whose name matches what the user typed in the search box
    private async Task<List<string>> StationIdsNamed(string search)
    {
        var filter = Builders<SolarStation>.Filter.Regex(
            s => s.Name, new BsonRegularExpression(Regex.Escape(search), "i"));

        var matches = await stations.Find(filter).ToListAsync();
        return matches.Select(s => s.Id).ToList();
    }
}
