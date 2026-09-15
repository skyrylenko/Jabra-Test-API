using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Contracts;
using RoomBooking.Infrastructure;

namespace RoomBooking.Controllers;

[ApiController]
[Route("api/rooms")]
public sealed class RoomsController(RoomBookingDbContext dbContext) : ControllerBase
{
	/// <summary>
	/// Returns all rooms or only rooms available during the requested time range.
	/// </summary>
	/// <param name="query">
	/// Optional availability interval. Both <c>from</c> and <c>to</c> must be provided together.
	/// </param>
	/// <param name="cancellationToken">Request cancellation token.</param>
	/// <returns>A list of rooms matching the requested availability criteria.</returns>
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
					// Adjacent reservations are allowed; only overlapping ranges conflict.
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

	/// <summary>
	/// Returns reservations for a room, optionally filtered by a time range.
	/// </summary>
	/// <param name="roomId">The identifier of the room.</param>
	/// <param name="query">Optional reservation time range.</param>
	/// <param name="cancellationToken">Request cancellation token.</param>
	/// <returns>A list of reservations for the specified room.</returns>
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

	/// <summary>
	/// Creates a reservation if the room exists and the requested time is available.
	/// </summary>
	/// <param name="roomId">The identifier of the room to reserve.</param>
	/// <param name="request">Reservation title and requested time range.</param>
	/// <param name="cancellationToken">Request cancellation token.</param>
	/// <returns>The newly created reservation.</returns>
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
				// Adjacent reservations are allowed; only overlapping ranges conflict.
				reservation.Start < request.End &&
				reservation.End > request.Start,
				cancellationToken);

		if (hasConflict)
		{
			return Conflict("The room is already reserved for the requested time range.");
		}

		var reservation = new Domain.Reservation
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
