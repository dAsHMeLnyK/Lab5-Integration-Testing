using Microsoft.EntityFrameworkCore;
using Lab5.Api.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try {
        var db = services.GetRequiredService<AppDbContext>();
        
        int retries = 5;
        while (retries > 0) {
            try {
                logger.LogInformation("Attempting to apply migrations/EnsureCreated...");
                db.Database.EnsureCreated();
                logger.LogInformation("Database is ready.");
                break;
            }
            catch (Exception ex) {
                retries--;
                logger.LogWarning($"Database not ready yet. Retrying... ({retries} attempts left)");
                if (retries == 0) throw;
                Thread.Sleep(2000);
            }
        }
    }
    catch (Exception ex) {
        logger.LogCritical(ex, "An error occurred while creating the DB.");
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();