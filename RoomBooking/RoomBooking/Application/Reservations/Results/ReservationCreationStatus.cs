namespace RoomBooking.Application.Reservations.Results;

public enum ReservationCreationStatus
{
	Created,
	InvalidTimeRange,
	RoomNotFound,
	Conflict
}