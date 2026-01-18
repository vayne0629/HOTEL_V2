using HotelAPI_V2.Models;

namespace HotelAPI_V2.Repositories
{
    public interface ICustomerRepository
    {
        Task<List<CustomerSearchResultDto>> SearchAsync(string? keyword);
        Task<CustomerDetailDto?> GetByIdAsync(long id);
        Task<CustomerDetailDto?> GetByIdNumberAsync(string idNumber);
        Task<List<CustomerDetailDto>> SearchByFieldAsync(string field, string keyword);
    }
}

