using ElectronicStore.WebApi.Domain.Entities;
using ElectronicStore.WebApi.Domain.Model;
using ElectronicStore.WebApi.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicStore.WebApi.Infrastructure.AuthenticationService
{
    public class TokenHandler : ITokenHandler
    {
        IConfiguration _configuration;
        IUserService _userService;
        IUserTokenService _userTokenService;
        public TokenHandler(IConfiguration configuration, IUserService userService, IUserTokenService userTokenService)
        {
            _userTokenService = userTokenService;
            _configuration = configuration;
            _userService = userService;
        }
        public async Task<(string, DateTime)> CreateAccessToken(User user)
        {
            DateTime expiredAccessToken = DateTime.UtcNow.AddSeconds(15);
            var claims = new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.DisplayName),
                    new Claim("UserName", user.UserName),
                    new Claim("TokenId", Guid.NewGuid().ToString())
                };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["TokenBear:SignatureKey"]));
            var Credentails = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var tokenInfo = new JwtSecurityToken(
                issuer: _configuration["TokenBear:Issuer"],
                audience: _configuration["TokenBear:Audience"],
                claims: claims,
                expires: expiredAccessToken,
                signingCredentials: Credentails
                );
            string token = new JwtSecurityTokenHandler().WriteToken(tokenInfo);
            return await Task.FromResult((token, expiredAccessToken));
        }
        public async Task<(string, string, DateTime)> CreateRefreshToken(User user)
        {
            DateTime expiredRefreshToken = DateTime.UtcNow.AddHours(1);
            string code = Guid.NewGuid().ToString();
            var claims = new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.DisplayName),
                    new Claim("UserName", user.UserName),
                    new Claim("TokenId",code )
                };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["TokenBear:SignatureKey"]));
            var Credentails = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var tokenInfo = new JwtSecurityToken(
                issuer: _configuration["TokenBear:Issuer"],
                audience: _configuration["TokenBear:Audience"],
                claims: claims,
                expires: expiredRefreshToken,
                signingCredentials: Credentails
                );
            string refreshToken = new JwtSecurityTokenHandler().WriteToken(tokenInfo);
            return await Task.FromResult((refreshToken, code, expiredRefreshToken));
        }
        //public async Task<string> CreateToken(User user)
        //{
        //    var jwtTokenHandler = new JwtSecurityTokenHandler();

        //    var secretKeyBytes = Encoding.UTF8.GetBytes(_configuration["TokenBear:SignatureKey"]);

        //    var tokenDescription = new SecurityTokenDescriptor
        //    {
        //        Subject = new ClaimsIdentity(new[] {
        //            new Claim(ClaimTypes.Name, user.DisplayName),
        //            new Claim("UserName", user.UserName),
        //            new Claim("TokenId", Guid.NewGuid().ToString())

        //        }),
        //        Expires = DateTime.UtcNow.AddSeconds(20),
        //        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(
        //            Encoding.UTF8.GetBytes(_configuration["TokenBear:SignatureKey"])), SecurityAlgorithms.HmacSha256)
        //    };

        //    var token = jwtTokenHandler.CreateToken(tokenDescription);

        //    return jwtTokenHandler.WriteToken(token);
        //}
        public async Task ValidateToken(TokenValidatedContext context)
        {
            var claims = context.Principal.Claims.ToList();
            if (claims.Count == 0)
            {
                context.Fail("this token contain no information");
            }
            var identity = context.Principal.Identity as ClaimsIdentity;
            if (identity.FindFirst(JwtRegisteredClaimNames.Iss) == null)
            {
                context.Fail("This token is not issued by point entry");
                return;
            }
            if (identity.FindFirst("Username") == null)
            {
                string username = identity.FindFirst("Username").Value;
                var user = await _userService.FindByUserName(username);
                if (user == null)
                {
                    context.Fail("This token is invalid for user");

                    return;
                }

            }
            if (identity.FindFirst(JwtRegisteredClaimNames.Exp) == null)
            {
                var dateExp = identity.FindFirst(JwtRegisteredClaimNames.Exp);
            }
        }
        public async Task<JwtModel> ValidateRefreshToken(string refreshToken)
        {
            JwtModel jwtModel = new JwtModel();
            var cliamPriciple = new JwtSecurityTokenHandler().ValidateToken(refreshToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["TokenBear:Issuer"],
                ValidAudience = _configuration["TokenBear:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_configuration["TokenBear:SignatureKey"])),
                ClockSkew = TimeSpan.Zero
            },
               out _
            );
            if (cliamPriciple == null) return new();
            string code = cliamPriciple.Claims.FirstOrDefault(x => x.Type == "TokenId")?.Value;
            if (string.IsNullOrEmpty(code)) return new();
            UserToken userToken = await _userTokenService.CheckRefreshToken(code);
            if (userToken != null)
            {
                User user = await _userService.FindById(userToken.UserId);
                (string newAccessToken, DateTime createDate) = await CreateAccessToken(user);
                (string newRefreshToken, string codeRefreshToken, DateTime newDateCreated) = await CreateRefreshToken(user);
                return new JwtModel
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    FullName = user.DisplayName,
                    UserName = user.UserName
                };

            }
            return new();
        }
    }
}
