using Microsoft.EntityFrameworkCore;
using ElectronicStore.WebApi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicStore.WebApi.Infrastructure.Data
{
    public class ElectronicStoreContext : DbContext
    {
        public ElectronicStoreContext(DbContextOptions<ElectronicStoreContext> options) : base(options)
        {
        }
        public DbSet<Products> Products { get; set; }
        public DbSet<Categories> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserToken> UserTokens { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // You can configure your entity mappings here if needed
        }   
    }
}
