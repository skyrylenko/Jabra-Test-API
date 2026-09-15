namespace RoomBooking.Contracts;

public sealed record RoomAvailabilityQuery(
	DateTime? From,
	DateTime? To);