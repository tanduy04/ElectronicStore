using ElectronicStore.Api.Service.MailService;
using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;
using System.Net;
using System.Net.Mail;
using System.Text;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }


    public async Task SendForgotPasswordEmail(string email, string newPassword)
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
}
