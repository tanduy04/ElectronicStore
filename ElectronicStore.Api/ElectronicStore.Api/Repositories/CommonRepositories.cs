using ElectronicStore.Api.Data;
using ElectronicStore.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Repositories
{
    public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
    {
        public OrderDetailRepository(ElectronicStoreContext context) : base(context) { }

        public async Task<IEnumerable<OrderDetail>> GetByOrderIdAsync(int orderId)
        {
            return await _dbSet
                .Where(od => od.OrderId == orderId)
                .Include(od => od.Product)
                .ToListAsync();
        }
    }

    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(ElectronicStoreContext context) : base(context) { }
    }

    public class BrandRepository : Repository<Brand>, IBrandRepository
    {
        public BrandRepository(ElectronicStoreContext context) : base(context) { }
    }

    public class CartRepository : Repository<Cart>, ICartRepository
    {
        public CartRepository(ElectronicStoreContext context) : base(context) { }
    }

    public class VoucherRepository : Repository<Voucher>, IVoucherRepository
    {
        public VoucherRepository(ElectronicStoreContext context) : base(context) { }
    }

    public class FlashSaleRepository : Repository<FlashSale>, IFlashSaleRepository
    {
        public FlashSaleRepository(ElectronicStoreContext context) : base(context) { }
    }

    public class ProductReviewRepository : Repository<ProductReview>, IProductReviewRepository
    {
        public ProductReviewRepository(ElectronicStoreContext context) : base(context) { }
    }

    public class SupplierRepository : Repository<Supplier>, ISupplierRepository
    {
        public SupplierRepository(ElectronicStoreContext context) : base(context) { }
    }

    public class ImportRepository : Repository<Import>, IImportRepository
    {
        public ImportRepository(ElectronicStoreContext context) : base(context) { }
    }

    public class BannerRepository : Repository<Banner>, IBannerRepository
    {
        public BannerRepository(ElectronicStoreContext context) : base(context) { }
    }

    public class QuestionAndAnswerRepository : Repository<QuestionAndAnswer>, IQuestionAndAnswerRepository
    {
        public QuestionAndAnswerRepository(ElectronicStoreContext context) : base(context) { }
    }
}
