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

    // gets the reservation, station and user collections
    public ReservationViewService(IMongoDatabase db)
    {
        reservations = db.GetCollection<EnergyReservation>("EnergyReservation");
        stations = db.GetCollection<SolarStation>("SolarStationInfo");
        users = db.GetCollection<UserDetail>("UserDetail");
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
