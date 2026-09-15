namespace RoomBooking.Domain
{
	public sealed class Reservation
	{
		public int Id { get; set; }
		public int RoomId { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public required string Title { get; set; }
		public DateTime CreatedAt { get; set; }
		public Room? Room { get; set; }
	}
}