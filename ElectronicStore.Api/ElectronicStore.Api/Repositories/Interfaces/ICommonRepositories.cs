using ElectronicStore.Api.Data;

namespace ElectronicStore.Api.Repositories.Interfaces
{
    public interface ICategoryRepository : IRepository<Category> { }
    public interface IBrandRepository : IRepository<Brand> { }
    public interface ICartRepository : IRepository<Cart> { }
    public interface IVoucherRepository : IRepository<Voucher> { }
    public interface IFlashSaleRepository : IRepository<FlashSale> { }
    public interface IProductReviewRepository : IRepository<ProductReview> { }
    public interface ISupplierRepository : IRepository<Supplier> { }
    public interface IImportRepository : IRepository<Import> { }
    public interface IBannerRepository : IRepository<Banner> { }
    public interface IQuestionAndAnswerRepository : IRepository<QuestionAndAnswer> { }
}
