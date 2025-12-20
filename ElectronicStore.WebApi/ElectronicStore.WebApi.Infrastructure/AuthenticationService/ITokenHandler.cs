using ElectronicStore.WebApi.Domain.Entities;
using ElectronicStore.WebApi.Domain.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ElectronicStore.WebApi.Infrastructure.AuthenticationService
{
    public interface ITokenHandler
    {
        Task<(string, DateTime)> CreateAccessToken(User user);

        Task<(string,string, DateTime)> CreateRefreshToken(User user);
        Task<JwtModel> ValidateRefreshToken(string refreshToken);
        Task ValidateToken(TokenValidatedContext context);
    }
}