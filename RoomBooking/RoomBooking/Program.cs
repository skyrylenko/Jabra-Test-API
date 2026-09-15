using Microsoft.EntityFrameworkCore;
using RoomBooking.Domain;
using RoomBooking.Infrastructure;

namespace RoomBooking;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddControllers();
		builder.Services.AddOpenApi();

		builder.Services.AddDbContext<RoomBookingDbContext>(options =>
			options.UseSqlite(
				builder.Configuration.GetConnectionString("RoomBookingDatabase")));

		var app = builder.Build();

		using (var scope = app.Services.CreateScope())
		{
			var dbContext = scope.ServiceProvider
				.GetRequiredService<RoomBookingDbContext>();

			dbContext.Database.EnsureCreated();

			if (!dbContext.Rooms.Any())
			{
				dbContext.Rooms.AddRange(
					new Room
					{
						Name = "Small Room",
						Capacity = 4
					},
					new Room
					{
						Name = "Conference Room",
						Capacity = 10
					},
					new Room
					{
						Name = "Auditorium",
						Capacity = 50
					});

				dbContext.SaveChanges();
			}
		}

		if (app.Environment.IsDevelopment())
		{
			app.MapOpenApi();
		}

		app.UseHttpsRedirection();
		app.UseAuthorization();
		app.MapControllers();

		app.Run();
	}
}