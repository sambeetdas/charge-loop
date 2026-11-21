using ChargeLoop.Application.DTOs;
using ChargeLoop.Application.Interfaces;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;
using ChargeLoop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChargeLoop.Infrastructure.Services;

public class ReservationService : IReservationService
{
    private readonly ChargeLoopDbContext _context;

    public ReservationService(ChargeLoopDbContext context)
    {
        _context = context;
    }

    public async Task<ReservationDto> CreateReservationAsync(ReservationRequest request)
    {
        await using var tx = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync()
            : null;

        var station = await _context.Stations.FindAsync(request.StationId);
        if (station == null) throw new ArgumentException("Station not found");

        var driver = await _context.Drivers.FindAsync(request.DriverId);
        if (driver == null) throw new ArgumentException("Driver not found");

        // Cancel any existing active reservations for this driver
        var existing = await _context.Reservations
            .Where(r => r.DriverId == request.DriverId && r.Status == ReservationStatus.Active)
            .ToListAsync();
        foreach (var r in existing)
        {
            r.Status = ReservationStatus.Cancelled;
        }
        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var heldBatteryIds = await _context.Reservations
            .Where(r => r.StationId == station.Id
                && r.Status == ReservationStatus.Active
                && r.ExpiresAt > now
                && r.ReservedBatteryId != null)
            .Select(r => r.ReservedBatteryId!.Value)
            .ToListAsync();

        // Find an available charged & visible battery not currently held by another active reservation
        var battery = await _context.Batteries
            .FirstOrDefaultAsync(b => b.CurrentStationId == station.Id
                && b.Status == BatteryStatus.AvailableCharged
                && b.IsVisibleToDriver
                && !heldBatteryIds.Contains(b.Id));

        if (battery == null)
        {
            throw new InvalidOperationException("No unreserved active batteries available at this station right now.");
        }

        var reservation = new Reservation
        {
            DriverId = request.DriverId,
            StationId = request.StationId,
            ReservedBatteryId = battery.Id,
            OtpCode = string.Empty,
            ReservedAt = now,
            ExpiresAt = now.AddHours(2),
            Status = ReservationStatus.Active
        };

        await _context.Reservations.AddAsync(reservation);
        await _context.SaveChangesAsync();
        if (tx != null) await tx.CommitAsync();

        return new ReservationDto(
            reservation.Id,
            reservation.DriverId,
            reservation.StationId,
            station.Name,
            battery.Id,
            battery.SerialNumber,
            reservation.ReservedAt,
            reservation.ExpiresAt,
            reservation.Status,
            reservation.OtpCode
        );
    }

    public async Task<ReservationDto?> GetActiveReservationAsync(Guid driverId)
    {
        var res = await _context.Reservations
            .AsNoTracking()
            .Where(r => r.DriverId == driverId && r.Status == ReservationStatus.Active && r.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(r => r.ReservedAt)
            .FirstOrDefaultAsync();

        if (res == null) return null;

        var station = await _context.Stations.FindAsync(res.StationId);
        var battery = res.ReservedBatteryId.HasValue ? await _context.Batteries.FindAsync(res.ReservedBatteryId.Value) : null;

        return new ReservationDto(
            res.Id,
            res.DriverId,
            res.StationId,
            station?.Name ?? "Station",
            battery?.Id,
            battery?.SerialNumber,
            res.ReservedAt,
            res.ExpiresAt,
            res.Status,
            res.OtpCode
        );
    }

    public async Task<bool> CancelReservationAsync(Guid reservationId)
    {
        var res = await _context.Reservations.FindAsync(reservationId);
        if (res == null) return false;

        res.Status = ReservationStatus.Cancelled;
        await _context.SaveChangesAsync();
        return true;
    }
}
