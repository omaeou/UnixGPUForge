using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using UnixGPUForge.Daemon.Core.Interfaces;

namespace UnixGPUForge.Daemon.Endpoints;

public static class GpuEndpoints
{
    public static void MapGpuEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gpu");

        group.MapGet("/metrics", (IGpuProvider gpu) => Results.Ok(gpu.GetTelemetry()));

        group.MapPost("/powerlimit", (IGpuProvider gpu, PowerLimitRequest req) =>
        {
            var success = gpu.SetPowerLimit(req.Watts);
            return success ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false, message = "Error applying power limit" });
        });

        group.MapPost("/clocklock", (IGpuProvider gpu, ClockLockRequest req) =>
        {
            var success = gpu.SetCoreClockLimit(req.MaxClock);
            return success ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false, message = "Error locking core clock" });
        });

        group.MapPost("/clockreset", (IGpuProvider gpu) =>
        {
            var success = gpu.ResetCoreClockLimit();
            return success ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false, message = "Error resetting core clock" });
        });

        group.MapPost("/memclocklock", (IGpuProvider gpu, ClockLockRequest req) =>
        {
            var success = gpu.SetMemoryClockOffset(req.MaxClock);
            return success ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false, message = "Error locking memory clock" });
        });

        group.MapPost("/memclockreset", (IGpuProvider gpu) =>
        {
            var success = gpu.ResetMemoryClockOffset();
            return success ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false, message = "Error resetting memory clock" });
        });
    }
}

public record PowerLimitRequest(uint Watts);
public record ClockLockRequest(uint MaxClock);