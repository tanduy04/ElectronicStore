using ElectronicStore.WebApi.Infrastructure.Data;
using ElectronicStore.WebApi.Infrastructure.Data.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElectronicStore.WebApi.Infrastructure.Services;
using ElectronicStore.WebApi.Infrastructure.AuthenticationService;


namespace ElectronicStore.WebApi.Infrastructure.Configuration
{
    public static class ConfigurationService
    {
        public static void RegisterContextDb(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ElectronicStoreContext>(options => options
            .UseSqlServer(configuration.GetConnectionString("ElectronicStoreConnection"),
            options => options.MigrationsAssembly(typeof(ElectronicStoreContext).Assembly.FullName)));
        }
        public static void RegisterDI(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(IDapperHelper<>), typeof(DapperHelper<>));
            services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));


            services.AddScoped<ICategoryService,CategoryService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITokenHandler, TokenHandler>();
            services.AddScoped<IUserTokenService, UserTokenService>();






        }
    }
}
