using ChargeLoop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChargeLoop.Infrastructure.Persistence;

public class ChargeLoopDbContext : DbContext
{
    public ChargeLoopDbContext(DbContextOptions<ChargeLoopDbContext> options) : base(options) { }

    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<Battery> Batteries => Set<Battery>();
    public DbSet<SwapTransaction> Swaps => Set<SwapTransaction>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<DriverSubscription> DriverSubscriptions => Set<DriverSubscription>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
    public DbSet<PaymentTransaction> Transactions => Set<PaymentTransaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Drivers mapping
        modelBuilder.Entity<Driver>(b =>
        {
            b.ToTable("drivers");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.PhoneNumber).HasColumnName("phone_number");
            b.Property(x => x.FullName).HasColumnName("full_name");
            b.Property(x => x.LanguageCode).HasColumnName("language_code");
            b.Property(x => x.KycStatus).HasColumnName("kyc_status");
            b.Property(x => x.DrivingLicenseNumber).HasColumnName("driving_license_number");
            b.Property(x => x.AadhaarMasked).HasColumnName("aadhaar_masked");
            b.Property(x => x.WalletBalance).HasColumnName("wallet_balance").HasColumnType("numeric(12,2)");
            b.Property(x => x.ActiveSubscriptionId).HasColumnName("active_subscription_id");
            b.Property(x => x.Email).HasColumnName("email");
            b.Property(x => x.ReferralCode).HasColumnName("referral_code");
            b.Property(x => x.ReferredByCode).HasColumnName("referred_by_code");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");

            b.HasMany(x => x.Vehicles)
             .WithOne()
             .HasForeignKey(x => x.DriverId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.Subscriptions)
             .WithOne()
             .HasForeignKey(x => x.DriverId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Vehicles mapping
        modelBuilder.Entity<Vehicle>(b =>
        {
            b.ToTable("vehicles");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.DriverId).HasColumnName("driver_id");
            b.Property(x => x.RegistrationNumber).HasColumnName("registration_number");
            b.Property(x => x.Oem).HasColumnName("oem");
            b.Property(x => x.Model).HasColumnName("model");
            b.Property(x => x.BatteryCompatibilityType).HasColumnName("battery_compatibility_type");
            b.Property(x => x.LinkedAt).HasColumnName("linked_at");
        });

        // Partners mapping
        modelBuilder.Entity<Partner>(b =>
        {
            b.ToTable("partners");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.FullName).HasColumnName("full_name");
            b.Property(x => x.BusinessName).HasColumnName("business_name");
            b.Property(x => x.PhoneNumber).HasColumnName("phone_number");
            b.Property(x => x.Email).HasColumnName("email");
            b.Property(x => x.UpiId).HasColumnName("upi_id");
            b.Property(x => x.BankAccountNumber).HasColumnName("bank_account_number");
            b.Property(x => x.WalletBalance).HasColumnName("wallet_balance").HasColumnType("numeric(12,2)");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");

            b.HasMany(x => x.Stations)
             .WithOne()
             .HasForeignKey(x => x.PartnerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Stations mapping
        modelBuilder.Entity<Station>(b =>
        {
            b.ToTable("stations");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.PartnerId).HasColumnName("partner_id");
            b.Property(x => x.Name).HasColumnName("name");
            b.Property(x => x.Address).HasColumnName("address");
            b.Property(x => x.City).HasColumnName("city");
            b.Property(x => x.Latitude).HasColumnName("latitude");
            b.Property(x => x.Longitude).HasColumnName("longitude");
            b.Property(x => x.TotalBays).HasColumnName("total_bays");
            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.OpenTime).HasColumnName("open_time");
            b.Property(x => x.CloseTime).HasColumnName("close_time");
            b.Property(x => x.AvailableChargedBatteriesCount).HasColumnName("available_charged_batteries");
            b.Property(x => x.ChargingBatteriesCount).HasColumnName("charging_batteries");
            b.Property(x => x.EstimatedWaitTimeMinutes).HasColumnName("estimated_wait_minutes");
            b.Property(x => x.LastStatusUpdate).HasColumnName("last_status_update");
        });

        // Batteries mapping
        modelBuilder.Entity<Battery>(b =>
        {
            b.ToTable("batteries");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.SerialNumber).HasColumnName("serial_number");
            b.Property(x => x.ModelType).HasColumnName("model_type");
            b.Property(x => x.StateOfChargePercent).HasColumnName("state_of_charge_percent");
            b.Property(x => x.StateOfHealthPercent).HasColumnName("state_of_health_percent");
            b.Property(x => x.CellTemperatureCelsius).HasColumnName("cell_temperature_celsius");
            b.Property(x => x.CycleCount).HasColumnName("cycle_count");
            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.CurrentStationId).HasColumnName("current_station_id");
            b.Property(x => x.CurrentDriverId).HasColumnName("current_driver_id");
            b.Property(x => x.IsVisibleToDriver).HasColumnName("is_visible_to_driver");
            b.Property(x => x.LastTelemetryTimestamp).HasColumnName("last_telemetry_timestamp");
        });

        // Swaps mapping
        modelBuilder.Entity<SwapTransaction>(b =>
        {
            b.ToTable("swap_transactions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.DriverId).HasColumnName("driver_id");
            b.Property(x => x.StationId).HasColumnName("station_id");
            b.Property(x => x.PartnerId).HasColumnName("partner_id");
            b.Property(x => x.DepletedBatteryId).HasColumnName("depleted_battery_id");
            b.Property(x => x.DepletedBatterySoc).HasColumnName("depleted_battery_soc");
            b.Property(x => x.IssuedBatteryId).HasColumnName("issued_battery_id");
            b.Property(x => x.IssuedBatterySoc).HasColumnName("issued_battery_soc");
            b.Property(x => x.InitiatedAt).HasColumnName("initiated_at");
            b.Property(x => x.CompletedAt).HasColumnName("completed_at");
            b.Property(x => x.DurationSeconds).HasColumnName("duration_seconds");
            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.BaseFee).HasColumnName("base_fee").HasColumnType("numeric(10,2)");
            b.Property(x => x.TaxAmount).HasColumnName("tax_amount").HasColumnType("numeric(10,2)");
            b.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("numeric(10,2)");
            b.Property(x => x.PartnerCommissionEarned).HasColumnName("partner_commission_earned").HasColumnType("numeric(10,2)");
            b.Property(x => x.PaymentMethod).HasColumnName("payment_method");
            b.Property(x => x.InvoiceNumber).HasColumnName("invoice_number");
            b.Property(x => x.BillSentChannels).HasColumnName("bill_sent_channels");
            b.Property(x => x.Notes).HasColumnName("notes");
        });

        // Wallet transactions mapping
        modelBuilder.Entity<WalletTransaction>(b =>
        {
            b.ToTable("wallet_transactions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.AccountType).HasColumnName("account_type");
            b.Property(x => x.AccountId).HasColumnName("account_id");
            b.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(12,2)");
            b.Property(x => x.Type).HasColumnName("type");
            b.Property(x => x.Reason).HasColumnName("reason");
            b.Property(x => x.ReferenceId).HasColumnName("reference_id");
            b.Property(x => x.Timestamp).HasColumnName("timestamp");
        });

        // Subscription Plans mapping
        modelBuilder.Entity<SubscriptionPlan>(b =>
        {
            b.ToTable("subscription_plans");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Name).HasColumnName("name");
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.ValidityDays).HasColumnName("validity_days");
            b.Property(x => x.MaxSwapsAllowed).HasColumnName("max_swaps_allowed");
            b.Property(x => x.Price).HasColumnName("price").HasColumnType("numeric(10,2)");
            b.Property(x => x.IsActive).HasColumnName("is_active");
        });

        // Driver Subscriptions mapping
        modelBuilder.Entity<DriverSubscription>(b =>
        {
            b.ToTable("driver_subscriptions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.DriverId).HasColumnName("driver_id");
            b.Property(x => x.PlanId).HasColumnName("plan_id");
            b.Property(x => x.StartDate).HasColumnName("start_date");
            b.Property(x => x.EndDate).HasColumnName("end_date");
            b.Property(x => x.SwapsUsed).HasColumnName("swaps_used");
            b.Property(x => x.SwapsTotalAllowed).HasColumnName("swaps_total_allowed");
            b.Property(x => x.IsActive).HasColumnName("is_active");
        });

        // Reservations mapping
        modelBuilder.Entity<Reservation>(b =>
        {
            b.ToTable("reservations");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.DriverId).HasColumnName("driver_id");
            b.Property(x => x.StationId).HasColumnName("station_id");
            b.Property(x => x.ReservedBatteryId).HasColumnName("reserved_battery_id");
            b.Property(x => x.OtpCode).HasColumnName("otp_code");
            b.Property(x => x.ReservedAt).HasColumnName("reserved_at");
            b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            b.Property(x => x.Status).HasColumnName("status");
        });

        // Support Tickets mapping
        modelBuilder.Entity<SupportTicket>(b =>
        {
            b.ToTable("support_tickets");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.UserType).HasColumnName("user_type");
            b.Property(x => x.UserId).HasColumnName("user_id");
            b.Property(x => x.Category).HasColumnName("category");
            b.Property(x => x.Subject).HasColumnName("subject");
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        // OTP Verifications mapping
        modelBuilder.Entity<OtpVerification>(b =>
        {
            b.ToTable("otp_verifications");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.PhoneNumber).HasColumnName("phone_number");
            b.Property(x => x.OtpCode).HasColumnName("otp_code");
            b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            b.Property(x => x.IsUsed).HasColumnName("is_used");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        // Unified Financial Transactions mapping
        modelBuilder.Entity<PaymentTransaction>(b =>
        {
            b.ToTable("transactions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
            b.HasIndex(x => x.CorrelationId).IsUnique();
            b.Property(x => x.UserId).HasColumnName("user_id");
            b.Property(x => x.UserRole).HasColumnName("user_role");
            b.Property(x => x.StationId).HasColumnName("station_id");
            b.Property(x => x.SwapId).HasColumnName("swap_id");
            b.Property(x => x.TransactionType).HasColumnName("transaction_type");
            b.Property(x => x.PaymentMode).HasColumnName("payment_mode");
            b.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(12,2)");
            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.Notes).HasColumnName("notes");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        // End-to-End Audit Logs mapping
        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable("audit_logs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
            b.Property(x => x.EventName).HasColumnName("event_name");
            b.Property(x => x.ActorId).HasColumnName("actor_id");
            b.Property(x => x.ActorRole).HasColumnName("actor_role");
            b.Property(x => x.HttpMethod).HasColumnName("http_method");
            b.Property(x => x.Endpoint).HasColumnName("endpoint");
            b.Property(x => x.StatusCode).HasColumnName("status_code");
            b.Property(x => x.DurationMs).HasColumnName("duration_ms");
            b.Property(x => x.Details).HasColumnName("details");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
    }
}
