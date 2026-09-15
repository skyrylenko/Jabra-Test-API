using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Contracts;
using RoomBooking.Infrastructure;

namespace RoomBooking.Controllers;

[ApiController]
[Route("api/rooms")]
public sealed class RoomsController(RoomBookingDbContext dbContext) : ControllerBase
{
	[HttpGet]
	[ProducesResponseType(typeof(IReadOnlyCollection<RoomResponse>), StatusCodes.Status200OK)]
	public async Task<ActionResult<IReadOnlyCollection<RoomResponse>>> GetRooms(
		CancellationToken cancellationToken)
	{
		var rooms = await dbContext.Rooms
			.AsNoTracking()
			.OrderBy(room => room.Name)
			.Select(room => new RoomResponse(
				room.Id,
				room.Name,
				room.Capacity))
			.ToListAsync(cancellationToken);

		return Ok(rooms);
	}

	[HttpGet("{roomId:int}/reservations")]
	[ProducesResponseType(typeof(IReadOnlyCollection<ReservationResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<IReadOnlyCollection<ReservationResponse>>> GetReservations(
		int roomId,
		[FromQuery] ReservationQuery query,
		CancellationToken cancellationToken)
	{
		if (query.From.HasValue &&
			query.To.HasValue &&
			query.From >= query.To)
		{
			return BadRequest("'from' must be earlier than 'to'.");
		}

		var roomExists = await dbContext.Rooms
			.AsNoTracking()
			.AnyAsync(room => room.Id == roomId, cancellationToken);

		if (!roomExists)
		{
			return NotFound($"Room with id {roomId} was not found.");
		}

		var reservationsQuery = dbContext.Reservations
			.AsNoTracking()
			.Where(reservation => reservation.RoomId == roomId);

		if (query.From.HasValue)
		{
			reservationsQuery = reservationsQuery.Where(reservation =>
				reservation.End > query.From.Value);
		}

		if (query.To.HasValue)
		{
			reservationsQuery = reservationsQuery.Where(reservation =>
				reservation.Start < query.To.Value);
		}

		var reservations = await reservationsQuery
			.OrderBy(reservation => reservation.Start)
			.Select(reservation => new ReservationResponse(
				reservation.Id,
				reservation.RoomId,
				reservation.Start,
				reservation.End,
				reservation.Title,
				reservation.CreatedAt))
			.ToListAsync(cancellationToken);

		return Ok(reservations);
	}
}