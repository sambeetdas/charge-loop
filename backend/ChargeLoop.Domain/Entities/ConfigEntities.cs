namespace ChargeLoop.Domain.Entities;

public class PricingConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal BaseSwapFee { get; set; } = 100.00m;
    public decimal GstPercent { get; set; } = 18.00m;
    public decimal PartnerCommission { get; set; } = 25.00m;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class BatteryTypeConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Voltage { get; set; } = 48;
    public int CapacityAh { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}
