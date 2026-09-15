using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Contracts;

public sealed class CreateReservationRequest
{
	public DateTime Start { get; init; }

	public DateTime End { get; init; }

	[Required]
	[StringLength(300, MinimumLength = 1)]
	public string Title { get; init; } = string.Empty;
}