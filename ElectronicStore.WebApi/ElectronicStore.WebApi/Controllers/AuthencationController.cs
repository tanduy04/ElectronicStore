using Azure.Core;
using ElectronicStore.WebApi.Domain.Entities;
using ElectronicStore.WebApi.Domain.Model;
using ElectronicStore.WebApi.Infrastructure.AuthenticationService;
using ElectronicStore.WebApi.Infrastructure.Services;
using ElectronicStore.WebApi.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthencationController : ControllerBase
    {
        IUserService _userService;
        ITokenHandler _tokenHandler;
        IUserTokenService _userTokenService;
        public AuthencationController(IUserService userService, ITokenHandler tokenHandler, IUserTokenService userTokenService)
        {
            _userService = userService;
            _tokenHandler = tokenHandler;
            _userTokenService = userTokenService;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AccountModel accountModel)
        {
            if (accountModel == null)
            {
                return BadRequest("Invalid client request");
            }
            var user = await _userService.CheckLogin(accountModel.UserName, accountModel.Password);
            if (user == null)
            {
                return Unauthorized();
            }
            (string accessToken, DateTime expriedDate) accessToken = await _tokenHandler.CreateAccessToken(user);
            (string refreshToken, string code, DateTime expriedDate) refreshToken = await _tokenHandler.CreateRefreshToken(user);
            var userToken = await _userTokenService.UserExist(user.Id);
            if (userToken != null)
            {
                userToken.AccessToken = accessToken.accessToken;
                userToken.ExpiredDateAccessToken = accessToken.expriedDate;
                userToken.RefreshToken = refreshToken.refreshToken;
                userToken.ExpiredDateRefreshToken = refreshToken.expriedDate;
                userToken.isActive = true;
                userToken.codeRefreshTooken = refreshToken.code;
                userToken.CreatedDate = DateTime.Now;
                _userTokenService.UpdateUserToken(userToken);
            }
            else
            {
                await _userTokenService.SaveToken(new UserToken
                {
                    UserId = user.Id,
                    AccessToken = accessToken.accessToken,
                    ExpiredDateAccessToken = accessToken.expriedDate,
                    RefreshToken = refreshToken.refreshToken,
                    ExpiredDateRefreshToken = refreshToken.expriedDate,
                    isActive = true,
                    codeRefreshTooken = refreshToken.code,
                    CreatedDate = DateTime.Now,
                });
            }



            return Ok(new JwtModel
            {
                AccessToken = accessToken.accessToken,
                RefreshToken = refreshToken.refreshToken,
                FullName = user.DisplayName,
                UserName = user.UserName
            });

        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel token)
        {
            return Ok(await _tokenHandler.ValidateRefreshToken(token.RefreshToken));
        }

    }
}
