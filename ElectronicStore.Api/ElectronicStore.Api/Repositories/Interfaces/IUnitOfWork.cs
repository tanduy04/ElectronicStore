namespace ElectronicStore.Api.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IAccountRepository Accounts { get; }
        ICustomerRepository Customers { get; }
        IEmployeeRepository Employees { get; }
        IProductRepository Products { get; }
        IOrderRepository Orders { get; }
        IOrderDetailRepository OrderDetails { get; }
        ICategoryRepository Categories { get; }
        IBrandRepository Brands { get; }
        ICartRepository Carts { get; }
        IVoucherRepository Vouchers { get; }
        IFlashSaleRepository FlashSales { get; }
        IProductReviewRepository ProductReviews { get; }
        ISupplierRepository Suppliers { get; }
        IImportRepository Imports { get; }
        IBannerRepository Banners { get; }
        IQuestionAndAnswerRepository QuestionAndAnswers { get; }
        
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
