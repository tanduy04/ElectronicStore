using ElectronicStore.WebApi.Domain.Entities;
using ElectronicStore.WebApi.Infrastructure.Data.Abstract;
using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicStore.WebApi.Infrastructure.Services
{

    public class UserService : IUserService
    {
        IUnitOfWork _unitOfWork;
        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<User> CheckLogin(string username, string password)
        {
            return await _unitOfWork.RepositoryUser.GetSingleConditionAsync(x => x.UserName == username && x.Password == password);
        }
        public async Task<User> FindByUserName(string username)
        {
            return await _unitOfWork.RepositoryUser.GetSingleConditionAsync(x => x.UserName == username);
        }
        public async Task<User> FindById(int id)
        {
            return await _unitOfWork.RepositoryUser.GetSingleConditionAsync(x => x.Id == id);
        }
    }
}
