using Microsoft.EntityFrameworkCore;
using RoomBooking.Application.Reservations.Results;
using RoomBooking.Contracts;
using RoomBooking.Domain;
using RoomBooking.Infrastructure;

namespace RoomBooking.Application.Reservations;

public sealed class ReservationService(RoomBookingDbContext dbContext) : IReservationService
{
	public async Task<ReservationCreationResult> CreateAsync(
		int roomId,
		CreateReservationRequest request,
		CancellationToken cancellationToken)
	{
		if (request.Start >= request.End)
		{
			return new(ReservationCreationStatus.InvalidTimeRange);
		}

		var roomExists = await dbContext.Rooms
			.AsNoTracking()
			.AnyAsync(room => room.Id == roomId, cancellationToken);

		if (!roomExists)
		{
			return new(ReservationCreationStatus.RoomNotFound);
		}

		var hasConflict = await dbContext.Reservations
			.AnyAsync(reservation =>
				reservation.RoomId == roomId &&
				reservation.Start < request.End &&
				reservation.End > request.Start,
				cancellationToken);

		if (hasConflict)
		{
			return new(ReservationCreationStatus.Conflict);
		}

		var reservation = new Reservation
		{
			RoomId = roomId,
			Start = request.Start,
			End = request.End,
			Title = request.Title.Trim(),
			CreatedAt = DateTime.UtcNow
		};

		dbContext.Reservations.Add(reservation);
		await dbContext.SaveChangesAsync(cancellationToken);

		var response = new ReservationResponse(
			reservation.Id,
			reservation.RoomId,
			reservation.Start,
			reservation.End,
			reservation.Title,
			reservation.CreatedAt);

		return new(ReservationCreationStatus.Created, response);
	}
}