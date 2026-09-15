namespace RoomBooking.Contracts;

public sealed record ReservationResponse(
	int Id,
	int RoomId,
	DateTime Start,
	DateTime End,
	string Title,
	DateTime CreatedAt);