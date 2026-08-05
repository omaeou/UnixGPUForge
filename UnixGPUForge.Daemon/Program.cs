using UnixGPUForge.Daemon.Core.Interfaces;
using UnixGPUForge.Daemon.Core.Providers;
using UnixGPUForge.Daemon.Core.Services;
using UnixGPUForge.Daemon.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

builder.Services.AddSingleton<IGpuProvider, NvidiaGpuProvider>();

builder.Services.AddHostedService<AutoProfileService>();
builder.Services.AddHostedService<FanControlService>();

var app = builder.Build();

app.UseCors("AllowAll");

app.MapGpuEndpoints();
app.MapProfileEndpoints();
app.MapFanCurveEndpoints();

app.Run();