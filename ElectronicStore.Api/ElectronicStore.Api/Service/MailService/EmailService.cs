using ElectronicStore.Api.Data;
using ElectronicStore.Api.Service.MailService;
using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;
using System.Net;
using System.Net.Mail;
using System.Text;

public class EmailService
{
    private readonly ElectronicStoreContext _context;
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }


    public async Task SendForgotPasswordEmail(string email,string username, string newPassword)
    {
        var fromAddress = new MailAddress(_config["EmailSettings:SenderEmail"], _config["EmailSettings:SenderName"]);
        var toAddress = new MailAddress(email);
        string fromPassword = _config["EmailSettings:Password"];
        string subject = "Mật khẩu mới của bạn";

        // Nội dung email HTML
        string body = $@"
        <html>
        <body style='font-family: Arial; line-height: 1.6;'>
            <h3>Mật khẩu mới của bạn</h3>
            <p>Xin chào,</p>
            <p>Tên đăng nhập của bạn là: <b>{username}</b></p>
            <p>Mật khẩu mới của bạn là: <b>{newPassword}</b></p>
            <p style='color: red;'>Vui lòng đổi mật khẩu sau khi đăng nhập để bảo mật tài khoản.</p>
            <br/>
            <p>Trân trọng,<br/>Đội ngũ hỗ trợ Điện Máy Xanh</p>
        </body>
        </html>
    ";

        var smtp = new SmtpClient
        {
            Host = _config["EmailSettings:SmtpServer"], // Thay SMTP server của bạn
            Port = 587,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
        };

        using (var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = body,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true // Quan trọng: để hiển thị HTML
        })
        {
            await smtp.SendMailAsync(message);
        }
    }
    public async Task UpdateOrderStatus(string email, string orderCode, string newStatus)
    {
        var fromAddress = new MailAddress(_config["EmailSettings:SenderEmail"], _config["EmailSettings:SenderName"]);
        var toAddress = new MailAddress(email);
        string fromPassword = _config["EmailSettings:Password"];
        string subject = "Cập nhật trạng thái đơn hàng";

        // Chuyển trạng thái sang tiếng Việt
        string newStatusVi = GetVietnameseStatus(newStatus);

        // Nội dung email HTML
        string body = $@"
    <html>
    <body style='font-family: Arial; line-height: 1.6;'>
        <h3>Đơn hàng #{orderCode} của bạn đã thay đổi trạng thái</h3>
        <p>Xin chào,</p>
        <p>Trạng thái mới: <b>{newStatusVi}</b></p>
        <br/>
        <p>Trân trọng,<br/>Đội ngũ hỗ trợ Điện Máy Xanh</p>
    </body>
    </html>
";

        var smtp = new SmtpClient
        {
            Host = _config["EmailSettings:SmtpServer"],
            Port = 587,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
        };

        using (var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = body,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        })
        {
            await smtp.SendMailAsync(message);
        }
    }
    public async Task CreateOrderSuccess(string email, string orderCode)
    {
        var fromAddress = new MailAddress(_config["EmailSettings:SenderEmail"], _config["EmailSettings:SenderName"]);
        var toAddress = new MailAddress(email);
        string fromPassword = _config["EmailSettings:Password"];
        string subject = "Đặt hàng thành công";

        // Chuyển trạng thái sang tiếng Việt

        // Nội dung email HTML
        string body = $@"
    <html>
    <body style='font-family: Arial; line-height: 1.6;'>
        <p>Xin chào,</p>
        <p>Bạn đã đặt hàng thành công</p>
        <p>Mã đơn hàng: <b>#{orderCode}</b></p>
        <br/>
        <p>Trân trọng,<br/>Đội ngũ hỗ trợ Điện Máy Xanh</p>
    </body>
    </html>
";

        var smtp = new SmtpClient
        {
            Host = _config["EmailSettings:SmtpServer"],
            Port = 587,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
        };

        using (var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = body,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        })
        {
            await smtp.SendMailAsync(message);
        }
    }
    // Hàm chuyển đổi trạng thái sang tiếng Việt
    private string GetVietnameseStatus(string status)
    {
        return status switch
        {
            "Pending" => "Chờ xử lý",
            "Processing" => "Đang xử lý",
            "Shipping" => "Đang giao",
            "Delivered" => "Đã giao",
            "Cancelled" => "Đã hủy",
            _ => status // nếu không khớp giữ nguyên
        };
    }

}
