using RoomBooking.Application.Reservations.Results;
using RoomBooking.Contracts;

namespace RoomBooking.Application.Reservations;

public interface IReservationService
{
	Task<ReservationCreationResult> CreateAsync(
		int roomId,
		CreateReservationRequest request,
		CancellationToken cancellationToken);
}