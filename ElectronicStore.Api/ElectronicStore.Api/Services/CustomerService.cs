using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;

namespace ElectronicStore.Api.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CustomerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(bool Success, string Message, object? Data)> GetAllCustomersAsync()
        {
            try
            {
                var customers = await _unitOfWork.Customers.GetAllAsync();
                return (true, "Success", customers);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetCustomerByIdAsync(int id)
        {
            try
            {
                var customer = await _unitOfWork.Customers.GetCustomerWithAccountAsync(id);
                if (customer == null)
                    return (false, "Customer not found", null);

                return (true, "Success", customer);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetCustomerByAccountIdAsync(int accountId)
        {
            try
            {
                var customer = await _unitOfWork.Customers.GetByAccountIdAsync(accountId);
                if (customer == null)
                    return (false, "Customer not found", null);

                return (true, "Success", customer);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateCustomerAsync(int id, CustomerDto dto)
        {
            try
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(id);
                if (customer == null)
                    return (false, "Customer not found");

                customer.FullName = dto.FullName;
                customer.Phone = dto.Phone;
                customer.Address = dto.Address;
                customer.Gender = dto.Gender;
                customer.BirthDate = dto.BirthDate;

                _unitOfWork.Customers.Update(customer);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Customer updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }
    }
}
