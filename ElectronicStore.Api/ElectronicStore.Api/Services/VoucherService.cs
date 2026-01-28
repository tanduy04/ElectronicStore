using ElectronicStore.Api.Data;
using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Repositories.Interfaces;
using ElectronicStore.Api.Services.Interfaces;

namespace ElectronicStore.Api.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly IUnitOfWork _unitOfWork;

        public VoucherService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(bool Success, string Message, object? Data)> GetAllVouchersAsync()
        {
            try
            {
                var vouchers = await _unitOfWork.Vouchers.GetAllAsync();
                return (true, "Success", vouchers);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetVoucherByIdAsync(int id)
        {
            try
            {
                var voucher = await _unitOfWork.Vouchers.GetByIdAsync(id);
                if (voucher == null)
                    return (false, "Voucher not found", null);

                return (true, "Success", voucher);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> GetVoucherByCodeAsync(string code)
        {
            try
            {
                var voucher = await _unitOfWork.Vouchers.FirstOrDefaultAsync(v => v.VoucherCode == code);
                if (voucher == null)
                    return (false, "Voucher not found", null);

                return (true, "Success", voucher);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> CreateVoucherAsync(VoucherDto dto)
        {
            try
            {
                // Check if voucher code already exists
                var existing = await _unitOfWork.Vouchers.FirstOrDefaultAsync(v => v.VoucherCode == dto.VoucherCode);
                if (existing != null)
                    return (false, "Voucher code already exists", null);

                var voucher = new Voucher
                {
                    VoucherCode = dto.VoucherCode,
                    DiscountType = dto.DiscountType,
                    DiscountValue = dto.DiscountValue,
                    MinOrderValue = dto.MinOrderValue,
                    MaxDiscount = dto.MaxDiscount,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    UsageLimit = dto.UsageLimit,
                    UsedCount = 0,
                    IsActive = true
                };

                await _unitOfWork.Vouchers.AddAsync(voucher);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Voucher created successfully", voucher);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateVoucherAsync(int id, VoucherDto dto)
        {
            try
            {
                var voucher = await _unitOfWork.Vouchers.GetByIdAsync(id);
                if (voucher == null)
                    return (false, "Voucher not found");

                voucher.VoucherCode = dto.VoucherCode;
                voucher.DiscountType = dto.DiscountType;
                voucher.DiscountValue = dto.DiscountValue;
                voucher.MinOrderValue = dto.MinOrderValue;
                voucher.MaxDiscount = dto.MaxDiscount;
                voucher.StartDate = dto.StartDate;
                voucher.EndDate = dto.EndDate;
                voucher.UsageLimit = dto.UsageLimit;

                _unitOfWork.Vouchers.Update(voucher);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Voucher updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteVoucherAsync(int id)
        {
            try
            {
                var voucher = await _unitOfWork.Vouchers.GetByIdAsync(id);
                if (voucher == null)
                    return (false, "Voucher not found");

                // Soft delete
                voucher.IsActive = false;
                _unitOfWork.Vouchers.Update(voucher);
                await _unitOfWork.SaveChangesAsync();

                return (true, "Voucher deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, object? Data)> ValidateVoucherAsync(string code, decimal orderAmount)
        {
            try
            {
                var voucher = await _unitOfWork.Vouchers.FirstOrDefaultAsync(v => v.VoucherCode == code);

                if (voucher == null)
                    return (false, "Voucher not found", null);

                if (!voucher.IsActive)
                    return (false, "Voucher is not active", null);

                var now = DateTime.Now;
                if (voucher.StartDate > now)
                    return (false, "Voucher is not yet valid", null);

                if (voucher.EndDate < now)
                    return (false, "Voucher has expired", null);

                if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit.Value)
                    return (false, "Voucher usage limit reached", null);

                if (voucher.MinOrderValue.HasValue && orderAmount < voucher.MinOrderValue.Value)
                    return (false, $"Minimum order value is {voucher.MinOrderValue.Value}", null);

                // Calculate discount
                decimal discount = 0;
                if (voucher.DiscountType == "Percentage")
                {
                    discount = orderAmount * (voucher.DiscountValue / 100);
                    if (voucher.MaxDiscount.HasValue && discount > voucher.MaxDiscount.Value)
                        discount = voucher.MaxDiscount.Value;
                }
                else // Fixed amount
                {
                    discount = voucher.DiscountValue;
                }

                var result = new
                {
                    voucher.VoucherId,
                    voucher.VoucherCode,
                    voucher.DiscountType,
                    voucher.DiscountValue,
                    CalculatedDiscount = discount
                };

                return (true, "Voucher is valid", result);
            }
            catch (Exception ex)
            {
                return (false, $"Internal server error: {ex.Message}", null);
            }
        }
    }
}
