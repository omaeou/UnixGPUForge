using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using UnixGPUForge.Shared.Models;

namespace UnixGPUForge.Daemon.Endpoints;

public static class FanCurveEndpoints
{
    private const string FilePath = "fancurve.json";

    public static void MapFanCurveEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fancurve");

        group.MapGet("/", () =>
        {
            if (!File.Exists(FilePath))
            {
                var defaultCurve = new List<FanCurvePoint>
                {
                    new() { Temperature = 40, FanSpeedPercent = 0 },
                    new() { Temperature = 60, FanSpeedPercent = 40 },
                    new() { Temperature = 80, FanSpeedPercent = 80 },
                    new() { Temperature = 90, FanSpeedPercent = 100 }
                };
                return Results.Ok(defaultCurve);
            }

            var json = File.ReadAllText(FilePath);
            var curve = JsonSerializer.Deserialize<List<FanCurvePoint>>(json) ?? new List<FanCurvePoint>();
            return Results.Ok(curve);
        });

        group.MapPost("/", (List<FanCurvePoint> curve) =>
        {
            var sortedCurve = curve.OrderBy(p => p.Temperature).ToList();
            var json = JsonSerializer.Serialize(sortedCurve, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
            return Results.Ok(new { success = true });
        });
    }
}