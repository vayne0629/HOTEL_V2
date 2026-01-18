using HotelAPI_V2.Models;
using static HotelAPI_V2.Controllers.CustomersController;

namespace HotelAPI_V2.Services
{
    public interface ICustomerService
    {
        Task<List<CustomerSearchResultDto>> SearchAsync(string? keyword);
        Task<CustomerDetailDto?> GetByIdAsync(long id);
        Task<CustomerDetailDto?> GetByIdNumberAsync(string idNumber);
        Task<List<CustomerDetailDto>> SearchByFieldAsync(string field, string keyword);
        Task<bool> UpdateAsync(long id, CustomerUpdateRequest req);
    }
}
