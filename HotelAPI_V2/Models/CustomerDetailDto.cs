namespace HotelAPI_V2.Models
{
    public class CustomerDetailDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string IDNumber { get; set; } = "";
        public string DateOfBirth { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Phone2 { get; set; } = "";
        public string TaxID { get; set; } = "";
        public string CarNumber1 { get; set; } = "";
        public string CarNumber2 { get; set; } = "";
        public bool Blacklist { get; set; }
        public string Habit { get; set; } = "";
        public string BookingSource { get; set; } = "";
        public bool Line { get; set; }
        public string BLReason { get; set; } = "";
        public string Company { get; set; } = "";
    }
}
