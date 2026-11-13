using ChargeLoop.Domain.Enums;

namespace ChargeLoop.Domain.Entities;

public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DriverId { get; set; }
    public Guid StationId { get; set; }
    public Guid? ReservedBatteryId { get; set; }
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(15);
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
}

public class SupportTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public WalletAccountType UserType { get; set; }
    public Guid UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
