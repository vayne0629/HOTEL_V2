namespace HotelAPI_V2.Models
{
    public class QrCompleteRequest
    {
        public string RoomNumber { get; set; } = "";
        public string AreaCode { get; set; } = "";
        public long? CleanerId { get; set; } = null;     // ✅ bigint 對應 long?
        public string? CleanerName { get; set; } = null; // 先可空
    }
}
