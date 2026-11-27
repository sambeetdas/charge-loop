using System.Text.Json;
using ChargeLoop.Application.DTOs;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChargeLoop.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/config")]
public class ConfigController : ControllerBase
{
    private readonly ChargeLoopDbContext _context;
    private readonly IConfiguration _configuration;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public ConfigController(ChargeLoopDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("pricing")]
    public async Task<ActionResult<PricingConfigDto>> GetPricing([FromQuery] int paymentMethod = 3)
    {
        var cfg = await _context.PricingConfigs
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.UpdatedAt)
            .FirstOrDefaultAsync() ?? new PricingConfig();

        var fare = FareCalculator.Calculate(cfg, (Domain.Enums.PaymentMethod)paymentMethod);

        return Ok(new PricingConfigDto(
            fare.BaseSwapFee, fare.PlatformFeePercent, fare.PlatformFee,
            fare.ConvenienceFee, fare.PaymentGatewayFee, fare.BankCharges,
            fare.GstPercent, fare.TaxAmount, fare.TotalAmount, fare.PartnerCommission,
            cfg.UpdatedAt));
    }

    [Authorize]
    [HttpPut("pricing")]
    public async Task<ActionResult<PricingConfigDto>> UpdatePricing([FromBody] UpdatePricingConfigRequest request)
    {
        if (request.BaseSwapFee < 0 || request.PlatformFeePercent < 0 || request.ConvenienceFee < 0 ||
            request.PaymentGatewayFee < 0 || request.BankCharges < 0 || request.GstPercent < 0 || request.PartnerCommission < 0)
            return BadRequest(new { Error = "Pricing values must be non-negative." });

        var existing = await _context.PricingConfigs.Where(p => p.IsActive).ToListAsync();
        foreach (var item in existing)
        {
            item.IsActive = false;
        }

        var next = new PricingConfig
        {
            BaseSwapFee = request.BaseSwapFee,
            PlatformFeePercent = request.PlatformFeePercent,
            ConvenienceFee = request.ConvenienceFee,
            PaymentGatewayFee = request.PaymentGatewayFee,
            BankCharges = request.BankCharges,
            GstPercent = request.GstPercent,
            PartnerCommission = request.PartnerCommission,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.PricingConfigs.AddAsync(next);
        await _context.SaveChangesAsync();

        var fare = FareCalculator.Calculate(next, Domain.Enums.PaymentMethod.Cash);
        return Ok(new PricingConfigDto(
            fare.BaseSwapFee, fare.PlatformFeePercent, fare.PlatformFee,
            fare.ConvenienceFee, fare.PaymentGatewayFee, fare.BankCharges,
            fare.GstPercent, fare.TaxAmount, fare.TotalAmount, fare.PartnerCommission,
            next.UpdatedAt));
    }

    [HttpGet("battery-types")]
    public async Task<ActionResult<IReadOnlyList<BatteryTypeDto>>> GetBatteryTypes()
    {
        var types = await _context.BatteryTypes
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Code)
            .Select(b => new BatteryTypeDto(b.Id, b.Code, b.DisplayName, b.Voltage, b.CapacityAh, b.IsActive))
            .ToListAsync();

        return Ok(types);
    }

    [HttpGet("geocode")]
    public async Task<ActionResult<GeocodeResultDto>> GeocodeAddress([FromQuery] string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return BadRequest(new { Error = "Address query is required." });

        var suggestions = await FetchAddressSuggestionsAsync(address, limit: 1);
        if (suggestions.Count == 0)
            return NotFound(new { Error = "Address not found. Try a more specific street/city." });

        return Ok(suggestions[0]);
    }

    /// <summary>
    /// Returns up to 5 address autocomplete suggestions with coordinates and city.
    /// </summary>
    [HttpGet("address-suggestions")]
    public async Task<ActionResult<List<GeocodeResultDto>>> GetAddressSuggestions([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 3)
            return Ok(new List<GeocodeResultDto>());

        var suggestions = await FetchAddressSuggestionsAsync(query.Trim(), limit: 5);
        return Ok(suggestions);
    }

    private async Task<List<GeocodeResultDto>> FetchAddressSuggestionsAsync(string query, int limit)
    {
        var results = new List<GeocodeResultDto>();
        var googleKey = _configuration["GoogleMaps:ApiKey"] ?? Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");
        if (!string.IsNullOrWhiteSpace(googleKey))
        {
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(query)}&key={Uri.EscapeDataString(googleKey)}";
            using var res = await _http.GetAsync(url);
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("status", out var st) && st.GetString() == "OK" &&
                    root.TryGetProperty("results", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var formatted = item.GetProperty("formatted_address").GetString() ?? query;
                        var loc = item.GetProperty("geometry").GetProperty("location");
                        var lat = loc.GetProperty("lat").GetDouble();
                        var lng = loc.GetProperty("lng").GetDouble();
                        var city = ExtractGoogleCity(item);
                        var street = ExtractGoogleStreetAddress(item, city, formatted);
                        results.Add(new GeocodeResultDto(formatted, city, lat, lng, "GoogleMaps", street));
                        if (results.Count >= limit) return results;
                    }
                    if (results.Count > 0) return results;
                }
            }
        }

        using var req = new HttpRequestMessage(HttpMethod.Get, $"https://nominatim.openstreetmap.org/search?format=json&limit={limit}&addressdetails=1&q={Uri.EscapeDataString(query)}");
        req.Headers.UserAgent.ParseAdd("ChargeLoop-MVP/1.0");
        using var osmRes = await _http.SendAsync(req);
        if (!osmRes.IsSuccessStatusCode) return results;

        var osmJson = await osmRes.Content.ReadAsStringAsync();
        using var osmDoc = JsonDocument.Parse(osmJson);
        foreach (var hit in osmDoc.RootElement.EnumerateArray())
        {
            var osmLat = double.Parse(hit.GetProperty("lat").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
            var osmLng = double.Parse(hit.GetProperty("lon").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
            var osmDisplay = hit.GetProperty("display_name").GetString() ?? query;
            var osmCity = ExtractOsmCity(hit);
            var osmStreet = ExtractOsmStreetAddress(hit, osmCity, osmDisplay);
            results.Add(new GeocodeResultDto(osmDisplay, osmCity, osmLat, osmLng, "OpenStreetMapFallback", osmStreet));
        }
        return results;
    }

    private static string ExtractGoogleCity(JsonElement item)
    {
        if (!item.TryGetProperty("address_components", out var comps)) return "";
        foreach (var c in comps.EnumerateArray())
        {
            if (!c.TryGetProperty("types", out var types)) continue;
            foreach (var t in types.EnumerateArray())
            {
                var typeName = t.GetString();
                if (typeName == "locality" || typeName == "administrative_area_level_2")
                    return c.GetProperty("long_name").GetString() ?? "";
            }
        }
        return "";
    }

    private static string ExtractGoogleStreetAddress(JsonElement item, string city, string formattedFallback)
    {
        var parts = new List<string>();
        if (item.TryGetProperty("address_components", out var comps))
        {
            string? streetNum = null;
            string? route = null;
            var localities = new List<string>();

            foreach (var c in comps.EnumerateArray())
            {
                if (!c.TryGetProperty("types", out var types)) continue;
                var longName = c.GetProperty("long_name").GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(longName)) continue;

                foreach (var t in types.EnumerateArray())
                {
                    var typeName = t.GetString();
                    if (typeName == "street_number") streetNum = longName;
                    else if (typeName == "route") route = longName;
                    else if (typeName is "premise" or "subpremise" or "sublocality_level_2" or "sublocality_level_1" or "neighborhood")
                        localities.Add(longName);
                }
            }

            var streetLine = string.Join(" ", new[] { streetNum, route }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (!string.IsNullOrWhiteSpace(streetLine)) parts.Add(streetLine);
            foreach (var loc in localities)
            {
                if (!string.Equals(loc, city, StringComparison.OrdinalIgnoreCase) &&
                    !parts.Any(p => string.Equals(p, loc, StringComparison.OrdinalIgnoreCase)))
                {
                    parts.Add(loc);
                }
            }
        }

        if (parts.Count > 0) return string.Join(", ", parts);
        return TrimBeforeCity(formattedFallback, city);
    }

    private static string ExtractOsmCity(JsonElement hit)
    {
        if (!hit.TryGetProperty("address", out var addrObj)) return "";
        if (addrObj.TryGetProperty("city", out var c1)) return c1.GetString() ?? "";
        if (addrObj.TryGetProperty("town", out var c2)) return c2.GetString() ?? "";
        if (addrObj.TryGetProperty("village", out var c3)) return c3.GetString() ?? "";
        if (addrObj.TryGetProperty("state_district", out var c4)) return c4.GetString() ?? "";
        return "";
    }

    private static string ExtractOsmStreetAddress(JsonElement hit, string city, string displayFallback)
    {
        var parts = new List<string>();
        if (hit.TryGetProperty("address", out var addrObj))
        {
            var houseNumber = GetJsonString(addrObj, "house_number");
            var road = GetJsonString(addrObj, "road")
                ?? GetJsonString(addrObj, "pedestrian")
                ?? GetJsonString(addrObj, "footway")
                ?? GetJsonString(addrObj, "residential")
                ?? GetJsonString(addrObj, "amenity")
                ?? GetJsonString(addrObj, "shop")
                ?? GetJsonString(addrObj, "building");

            var streetLine = string.Join(" ", new[] { houseNumber, road }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (!string.IsNullOrWhiteSpace(streetLine)) parts.Add(streetLine);

            foreach (var key in new[] { "neighbourhood", "quarter", "suburb", "hamlet" })
            {
                var val = GetJsonString(addrObj, key);
                if (!string.IsNullOrWhiteSpace(val) &&
                    !string.Equals(val, city, StringComparison.OrdinalIgnoreCase) &&
                    !parts.Any(p => string.Equals(p, val, StringComparison.OrdinalIgnoreCase)))
                {
                    parts.Add(val);
                }
            }
        }

        if (parts.Count > 0) return string.Join(", ", parts);
        return TrimBeforeCity(displayFallback, city);
    }

    private static string? GetJsonString(JsonElement obj, string prop)
        => obj.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()?.Trim()
            : null;

    private static string TrimBeforeCity(string fullAddress, string city)
    {
        if (string.IsNullOrWhiteSpace(fullAddress)) return "";
        var segments = fullAddress.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0) return fullAddress;

        if (!string.IsNullOrWhiteSpace(city))
        {
            var beforeCity = new List<string>();
            foreach (var seg in segments)
            {
                if (string.Equals(seg, city, StringComparison.OrdinalIgnoreCase)) break;
                beforeCity.Add(seg);
            }
            if (beforeCity.Count > 0 && beforeCity.Count < segments.Length)
                return string.Join(", ", beforeCity.Take(3));
        }

        return string.Join(", ", segments.Take(2));
    }
}
