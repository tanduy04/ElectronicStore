using ElectronicStore.Api.Data;
using ElectronicStore.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElectronicStore.Api.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ElectronicStoreContext _context;
        private IDbContextTransaction? _transaction;

        public IAccountRepository Accounts { get; }
        public ICustomerRepository Customers { get; }
        public IEmployeeRepository Employees { get; }
        public IProductRepository Products { get; }
        public IOrderRepository Orders { get; }
        public IOrderDetailRepository OrderDetails { get; }
        public ICategoryRepository Categories { get; }
        public IBrandRepository Brands { get; }
        public ICartRepository Carts { get; }
        public IVoucherRepository Vouchers { get; }
        public IFlashSaleRepository FlashSales { get; }
        public IProductReviewRepository ProductReviews { get; }
        public ISupplierRepository Suppliers { get; }
        public IImportRepository Imports { get; }
        public IBannerRepository Banners { get; }
        public IQuestionAndAnswerRepository QuestionAndAnswers { get; }

        public UnitOfWork(ElectronicStoreContext context)
        {
            _context = context;
            Accounts = new AccountRepository(_context);
            Customers = new CustomerRepository(_context);
            Employees = new EmployeeRepository(_context);
            Products = new ProductRepository(_context);
            Orders = new OrderRepository(_context);
            OrderDetails = new OrderDetailRepository(_context);
            Categories = new CategoryRepository(_context);
            Brands = new BrandRepository(_context);
            Carts = new CartRepository(_context);
            Vouchers = new VoucherRepository(_context);
            FlashSales = new FlashSaleRepository(_context);
            ProductReviews = new ProductReviewRepository(_context);
            Suppliers = new SupplierRepository(_context);
            Imports = new ImportRepository(_context);
            Banners = new BannerRepository(_context);
            QuestionAndAnswers = new QuestionAndAnswerRepository(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
