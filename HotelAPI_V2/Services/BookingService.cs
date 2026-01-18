using HotelAPI_V2.Models;
using HotelAPI_V2.Repositories;

namespace HotelAPI_V2.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _repo;

        public BookingService(IBookingRepository repo)
        {
            _repo = repo;
        }

        public Task<long?> CreateAsync(BookingCreateRequest req)
            => _repo.CreateAsync(req);

        public Task<bool> SoftDeleteAsync(long id, string deletedBy)
        => _repo.SoftDeleteAsync(id, deletedBy);
    }
}
