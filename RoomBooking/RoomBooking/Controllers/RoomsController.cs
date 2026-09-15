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
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<IReadOnlyCollection<RoomResponse>>> GetRooms(
	[FromQuery] RoomAvailabilityQuery query,
	CancellationToken cancellationToken)
	{
		if (query.From.HasValue != query.To.HasValue)
		{
			return BadRequest("'from' and 'to' must be provided together.");
		}

		if (query.From.HasValue && query.From >= query.To)
		{
			return BadRequest("'from' must be earlier than 'to'.");
		}

		var roomsQuery = dbContext.Rooms
			.AsNoTracking();

		if (query.From.HasValue && query.To.HasValue)
		{
			roomsQuery = roomsQuery.Where(room =>
				!room.Reservations.Any(reservation =>
					reservation.Start < query.To.Value &&
					reservation.End > query.From.Value));
		}

		var rooms = await roomsQuery
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


	[HttpPost("{roomId:int}/reservations")]
	[ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<ActionResult<ReservationResponse>> CreateReservation(
	int roomId,
	[FromBody] CreateReservationRequest request,
	CancellationToken cancellationToken)
	{
		if (request.Start >= request.End)
		{
			return BadRequest("'start' must be earlier than 'end'.");
		}

		var roomExists = await dbContext.Rooms
			.AsNoTracking()
			.AnyAsync(room => room.Id == roomId, cancellationToken);

		if (!roomExists)
		{
			return NotFound($"Room with id {roomId} was not found.");
		}

		var hasConflict = await dbContext.Reservations
			.AnyAsync(reservation =>
				reservation.RoomId == roomId &&
				reservation.Start < request.End &&
				reservation.End > request.Start,
				cancellationToken);

		if (hasConflict)
		{
			return Conflict("The room is already reserved for the requested time range.");
		}

		var reservation = new RoomBooking.Domain.Reservation
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

		return CreatedAtAction(
			nameof(GetReservations),
			new { roomId },
			response);
	}
}