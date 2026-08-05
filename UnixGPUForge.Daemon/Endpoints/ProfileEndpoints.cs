using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using UnixGPUForge.Shared.Models;

namespace UnixGPUForge.Daemon.Endpoints;

public static class ProfileEndpoints
{
    private const string FilePath = "profiles.json";

    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profiles");

        group.MapGet("/", () =>
        {
            if (!File.Exists(FilePath))
            {
                return Results.Ok(new List<GameProfile>());
            }

            var json = File.ReadAllText(FilePath);
            var profiles = JsonSerializer.Deserialize<List<GameProfile>>(json) ?? new List<GameProfile>();
            return Results.Ok(profiles);
        });

        group.MapPost("/", (List<GameProfile> profiles) =>
        {
            var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
            return Results.Ok(new { success = true });
        });
    }
}