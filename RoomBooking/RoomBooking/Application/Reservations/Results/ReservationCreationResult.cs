using RoomBooking.Contracts;

namespace RoomBooking.Application.Reservations.Results;

public sealed record ReservationCreationResult(
	ReservationCreationStatus Status,
	ReservationResponse? Reservation = null);