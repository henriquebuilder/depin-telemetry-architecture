using DePinCore.API.Hubs;
using DePinCore.Application.Interfaces;
using DePinCore.Application.Services;
using DePinCore.Domain.Services;
using DePinCore.Infrastructure.BackgroundServices;
using DePinCore.Infrastructure.Data;
using DePinCore.Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<INodeRepository, NodeRepository>();
builder.Services.AddScoped<ITelemetryRepository, TelemetryRepository>();
builder.Services.AddScoped<INodeHealthService, NodeHealthService>();
builder.Services.AddScoped<NodeHealthValidator>();

builder.Services.AddHostedService<TelemetryConsumerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHub<NodeHealthHub>("/hubs/nodehealth");

app.Run();
