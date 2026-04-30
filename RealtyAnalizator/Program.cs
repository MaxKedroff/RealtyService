using ParsingService.Extensions;
using Microsoft.EntityFrameworkCore;

using System;
using Infrastructure.Data;
using Infrastructure.Sync;
using Application.Interfaces;
using Application.Services;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.UseRelationalNulls();
        }
    ));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddParsers();
builder.Services.AddScoped<ISyncService, DatabaseSyncService>();
builder.Services.AddScoped<IMapService, MapService>();
builder.Services.AddScoped<IPolygonService, PolygonService>();
builder.Services.AddScoped<PredictionService>();
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var sql = dbContext.Database.GenerateCreateScript();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
