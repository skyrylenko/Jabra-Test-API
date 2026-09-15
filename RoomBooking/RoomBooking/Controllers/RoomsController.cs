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
}