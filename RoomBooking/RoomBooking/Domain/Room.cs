namespace RoomBooking.Domain
{
	public sealed class Room
	{
		public int Id { get; set; }

		public required string Name { get; set; }

		public int Capacity { get; set; }

		public ICollection<Reservation> Reservations { get; set; } = [];
	}
}
