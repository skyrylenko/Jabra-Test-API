using Microsoft.EntityFrameworkCore;
using RoomBooking.Domain;

namespace RoomBooking.Infrastructure;

public sealed class RoomBookingDbContext(DbContextOptions<RoomBookingDbContext> options)
	: DbContext(options)
{
	public DbSet<Room> Rooms => Set<Room>();

	public DbSet<Reservation> Reservations => Set<Reservation>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Room>(entity =>
		{
			entity.Property(room => room.Name)
				.HasMaxLength(200)
				.IsRequired();

			entity.HasIndex(room => room.Name)
				.IsUnique();

			entity.ToTable(tableBuilder =>
				tableBuilder.HasCheckConstraint(
					"CK_Room_Capacity_Positive",
					"\"Capacity\" > 0"));
		});

		modelBuilder.Entity<Reservation>(entity =>
		{
			entity.Property(reservation => reservation.Title)
				.HasMaxLength(300)
				.IsRequired();

			entity.Property(reservation => reservation.Start)
				.IsRequired();

			entity.Property(reservation => reservation.End)
				.IsRequired();

			entity.Property(reservation => reservation.CreatedAt)
				.IsRequired();

			// Supports room / time - range lookups when checking reservation conflicts.
			entity.HasIndex(reservation => new
			{
				reservation.RoomId,
				reservation.Start,
				reservation.End
			});

			entity.HasOne(reservation => reservation.Room)
				.WithMany(room => room.Reservations)
				.HasForeignKey(reservation => reservation.RoomId)
				.OnDelete(DeleteBehavior.Cascade);
			
			// Reservations must always have a positive duration.
			entity.ToTable(tableBuilder =>
				tableBuilder.HasCheckConstraint(
					"CK_Reservation_End_After_Start",
					"\"End\" > \"Start\""));
		});
	}
}