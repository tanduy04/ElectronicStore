using ElectronicStore.WebApi.Domain.Entities;
using ElectronicStore.WebApi.Infrastructure.Data.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicStore.WebApi.Infrastructure.Services
{
    public class UserTokenService : IUserTokenService
    {


        IRepository<UserToken> _userTokenRepository;
        public UserTokenService(IRepository<UserToken> userTokenRepository)
        {
            _userTokenRepository = userTokenRepository;
        }
        public async void UpdateUserToken(UserToken userToken)
        {
            _userTokenRepository.Update(userToken);
        }
        public async Task SaveToken(UserToken userToken)
        {
            await _userTokenRepository.Insert(userToken);
            await _userTokenRepository.Commit();
        }
        public async Task<UserToken> UserExist(int id)
        {
            return await _userTokenRepository.GetSingleConditionAsync(x => x.UserId == id);
        }
        public async Task<UserToken> CheckRefreshToken(string code)
        {
            return await _userTokenRepository.GetSingleConditionAsync(x => x.codeRefreshTooken == code);
        }
    }
}
