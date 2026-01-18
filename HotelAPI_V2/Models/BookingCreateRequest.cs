namespace HotelAPI_V2.Models
{
    public class BookingCreateRequest
    {
        public string IDNumber { get; set; } = "";
        public string? CheckInDate { get; set; }
        public string RoomNumber { get; set; } = "";
        public string? BookingSource { get; set; }
        public string? BookingName { get; set; }
        public decimal Amount { get; set; }
    }
}
