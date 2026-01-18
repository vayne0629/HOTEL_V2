using HotelAPI_V2.Models;

namespace HotelAPI_V2.Repositories
{
    public interface IBookingRepository
    {
        Task<long?> CreateAsync(BookingCreateRequest req);
        Task<bool> SoftDeleteAsync(long id, string deletedBy);
    }
}
