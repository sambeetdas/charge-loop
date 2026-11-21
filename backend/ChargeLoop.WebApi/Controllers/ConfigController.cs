using System.Text.Json;
using ChargeLoop.Application.DTOs;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChargeLoop.WebApi.Controllers;

[ApiController]
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

    [AllowAnonymous]
    [HttpGet("pricing")]
    public async Task<ActionResult<PricingConfigDto>> GetPricing()
    {
        var cfg = await _context.PricingConfigs
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.UpdatedAt)
            .FirstOrDefaultAsync();

        var baseFee = cfg?.BaseSwapFee ?? 100.00m;
        var gst = cfg?.GstPercent ?? 18.00m;
        var tax = Math.Round(baseFee * (gst / 100m), 2);
        var total = baseFee + tax;
        var comm = cfg?.PartnerCommission ?? 25.00m;

        return Ok(new PricingConfigDto(baseFee, gst, tax, total, comm, cfg?.UpdatedAt ?? DateTime.UtcNow));
    }

    [Authorize]
    [HttpPut("pricing")]
    public async Task<ActionResult<PricingConfigDto>> UpdatePricing([FromBody] UpdatePricingConfigRequest request)
    {
        if (request.BaseSwapFee < 0 || request.GstPercent < 0 || request.PartnerCommission < 0)
            return BadRequest(new { Error = "Pricing values must be non-negative." });

        var existing = await _context.PricingConfigs.Where(p => p.IsActive).ToListAsync();
        foreach (var item in existing)
        {
            item.IsActive = false;
        }

        var next = new PricingConfig
        {
            BaseSwapFee = request.BaseSwapFee,
            GstPercent = request.GstPercent,
            PartnerCommission = request.PartnerCommission,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.PricingConfigs.AddAsync(next);
        await _context.SaveChangesAsync();

        var tax = Math.Round(next.BaseSwapFee * (next.GstPercent / 100m), 2);
        return Ok(new PricingConfigDto(next.BaseSwapFee, next.GstPercent, tax, next.BaseSwapFee + tax, next.PartnerCommission, next.UpdatedAt));
    }

    [AllowAnonymous]
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

    [AllowAnonymous]
    [HttpGet("geocode")]
    public async Task<ActionResult<GeocodeResultDto>> GeocodeAddress([FromQuery] string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return BadRequest(new { Error = "Address query is required." });

        var googleKey = _configuration["GoogleMaps:ApiKey"] ?? Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");
        if (!string.IsNullOrWhiteSpace(googleKey))
        {
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={Uri.EscapeDataString(googleKey)}";
            using var res = await _http.GetAsync(url);
            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("status", out var st) && st.GetString() == "OK" &&
                root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
            {
                var first = results[0];
                var formatted = first.GetProperty("formatted_address").GetString() ?? address;
                var loc = first.GetProperty("geometry").GetProperty("location");
                var lat = loc.GetProperty("lat").GetDouble();
                var lng = loc.GetProperty("lng").GetDouble();
                var city = "";
                if (first.TryGetProperty("address_components", out var comps))
                {
                    foreach (var c in comps.EnumerateArray())
                    {
                        if (c.TryGetProperty("types", out var types))
                        {
                            foreach (var t in types.EnumerateArray())
                            {
                                if (t.GetString() == "locality" || t.GetString() == "administrative_area_level_2")
                                {
                                    city = c.GetProperty("long_name").GetString() ?? "";
                                    break;
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(city)) break;
                    }
                }
                return Ok(new GeocodeResultDto(formatted, city, lat, lng, "GoogleMaps"));
            }
        }

        // Dev fallback when GoogleMaps:ApiKey is not configured: OpenStreetMap Nominatim
        using var req = new HttpRequestMessage(HttpMethod.Get, $"https://nominatim.openstreetmap.org/search?format=json&limit=1&addressdetails=1&q={Uri.EscapeDataString(address)}");
        req.Headers.UserAgent.ParseAdd("ChargeLoop-MVP/1.0");
        using var osmRes = await _http.SendAsync(req);
        if (!osmRes.IsSuccessStatusCode)
            return BadRequest(new { Error = "Unable to geocode address. Configure GoogleMaps:ApiKey or enter coordinates manually." });

        var osmJson = await osmRes.Content.ReadAsStringAsync();
        using var osmDoc = JsonDocument.Parse(osmJson);
        if (osmDoc.RootElement.GetArrayLength() == 0)
            return NotFound(new { Error = "Address not found. Try a more specific street/city." });

        var hit = osmDoc.RootElement[0];
        var osmLat = double.Parse(hit.GetProperty("lat").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        var osmLng = double.Parse(hit.GetProperty("lon").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        var osmDisplay = hit.GetProperty("display_name").GetString() ?? address;
        var osmCity = "";
        if (hit.TryGetProperty("address", out var addrObj))
        {
            if (addrObj.TryGetProperty("city", out var c1)) osmCity = c1.GetString() ?? "";
            else if (addrObj.TryGetProperty("town", out var c2)) osmCity = c2.GetString() ?? "";
            else if (addrObj.TryGetProperty("state_district", out var c3)) osmCity = c3.GetString() ?? "";
        }
        return Ok(new GeocodeResultDto(osmDisplay, osmCity, osmLat, osmLng, "OpenStreetMapFallback"));
    }
}
