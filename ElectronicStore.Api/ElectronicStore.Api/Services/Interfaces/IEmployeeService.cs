using ElectronicStore.Api.Dto;
using Microsoft.AspNetCore.Http;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface IEmployeeService
    {
        Task<(bool Success, string Message, object? Data)> GetAllEmployeesAsync(int pageNumber, int pageSize);
        Task<(bool Success, string Message, object? Data)> GetEmployeeByIdAsync(int id);
        Task<(bool Success, string Message, object? Data)> GetEmployeeByAccountIdAsync(int accountId);
        Task<(bool Success, string Message, object? Data)> CreateEmployeeAsync(EmployeeDto dto, IFormFile? avatarFile);
        Task<(bool Success, string Message)> UpdateEmployeeAsync(int id, EmployeeDto dto, IFormFile? avatarFile);
        Task<(bool Success, string Message)> DeleteEmployeeAsync(int id);
    }
}
