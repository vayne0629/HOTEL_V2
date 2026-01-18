using HotelAPI_V2.Models;

namespace HotelAPI_V2.Services
{
    public interface IBookingService
    {
        Task<long?> CreateAsync(BookingCreateRequest req);
        Task<bool> SoftDeleteAsync(long id, string deletedBy);
    }
}
