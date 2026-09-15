namespace RoomBooking.Contracts;

public sealed record CreateReservationRequest(
	DateTime Start,
	DateTime End,
	string Title);