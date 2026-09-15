namespace RoomBooking.Contracts;

public sealed record ReservationQuery(
	DateTime? From,
	DateTime? To);