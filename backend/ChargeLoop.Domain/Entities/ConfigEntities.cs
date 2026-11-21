using ChargeLoop.Domain.Enums;

namespace ChargeLoop.Domain.Entities;

public class PricingConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal BaseSwapFee { get; set; } = 100.00m;
    public decimal PlatformFeePercent { get; set; } = 10.00m; // % of BaseSwapFee
    public decimal ConvenienceFee { get; set; } = 5.00m;
    public decimal PaymentGatewayFee { get; set; } = 2.50m;
    public decimal BankCharges { get; set; } = 1.50m;
    public decimal GstPercent { get; set; } = 18.00m;
    public decimal PartnerCommission { get; set; } = 25.00m;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public record FareBreakdown(
    decimal BaseSwapFee,
    decimal PlatformFeePercent,
    decimal PlatformFee,
    decimal ConvenienceFee,
    decimal PaymentGatewayFee,
    decimal BankCharges,
    decimal GstPercent,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PartnerCommission
);

public static class FareCalculator
{
    public static FareBreakdown Calculate(PricingConfig cfg, PaymentMethod paymentMethod)
    {
        var isCash = paymentMethod == PaymentMethod.Cash;
        var baseFee = cfg.BaseSwapFee;
        var platformFee = Math.Round(baseFee * (cfg.PlatformFeePercent / 100m), 2);
        var convenienceFee = cfg.ConvenienceFee;
        var gatewayFee = isCash ? 0.00m : cfg.PaymentGatewayFee;
        var bankCharges = isCash ? 0.00m : cfg.BankCharges;
        var taxable = baseFee + platformFee + convenienceFee + gatewayFee + bankCharges;
        var tax = Math.Round(taxable * (cfg.GstPercent / 100m), 2);
        var total = taxable + tax;

        return new FareBreakdown(
            baseFee,
            cfg.PlatformFeePercent,
            platformFee,
            convenienceFee,
            gatewayFee,
            bankCharges,
            cfg.GstPercent,
            tax,
            total,
            cfg.PartnerCommission
        );
    }
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
