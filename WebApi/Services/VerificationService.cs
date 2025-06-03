using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using WebApi.Models;

namespace WebApi.Services;

public class VerificationService(IConfiguration configuration, EmailClient emailClient, IMemoryCache cache) : IVerificationService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly EmailClient _emailClient = emailClient;
    private readonly IMemoryCache _cache = cache;
    private static readonly Random _random = new();

    public async Task<VerificationServiceResult> SendVerificationCodeAsync(SendVerificationCodeRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                return new VerificationServiceResult { Succeeded = false, Error = "Recipient email address is required." };
            }

            var verificationCode = _random.Next(100000, 999999).ToString();
            var subject = $"Your verification code is: {verificationCode}";
            var plainTextContent = $@"
            Verify Your Email Address

            Hello,

            Thank you for signing up!

            To complete your registration, please verify your email address using the code below:

            {verificationCode}

            If you did not request this, please ignore this email.

            Thanks,
            Ventixe
        ";

            var htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset='UTF-8'>
              <title>Verify Your Email Address</title>
            </head>
            <body style='font-family: Arial, sans-serif; background-color: #f6f8fa; margin: 0; padding: 0;'>
              <div style='max-width: 600px; margin: 40px auto; background-color: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);'>
                <h2 style='margin-top: 0;'>Verify Your Email Address</h2>
                <p style='margin-bottom: 15px;'>Hello,</p>
                <p style='margin-bottom: 15px;'>Thank you for signing up!</p>
                <p style='margin-bottom: 15px;'>To complete your registration, please verify your email address using the code below:</p>
                <div style='font-size: 24px; font-weight: bold; background-color: #f0f0f0; padding: 15px; text-align: center; border-radius: 6px; margin: 20px 0;'>{verificationCode}</div>
                <p style='margin-bottom: 15px;'>If you did not request this, please ignore this email.</p>
                <div style='margin-top: 30px; font-size: 14px; color: #555555;'>
                  <p>Thanks,<br>Ventixe</p>
                </div>
              </div>
            </body>
            </html>
        ";

            var emailMessage = new EmailMessage(
                senderAddress: _configuration["ACS:SenderAddress"],
                recipients: new EmailRecipients([new(request.Email)]),
                content: new EmailContent(subject)
                {
                    PlainText = plainTextContent,
                    Html = htmlContent
                }
            );

            var emailSendOperation = await _emailClient.SendAsync(WaitUntil.Started, emailMessage);
            SaveVerificationCode(new SaveVerificationCodeRequest
            {
                Email = request.Email,
                Code = verificationCode,
                ValidFor = TimeSpan.FromMinutes(5)
            });

            return new VerificationServiceResult
            {
                Succeeded = true,
                Message = "Verification code sent successfully!"
            };
        }
        catch (Exception ex)
        {
            Debug.Write(ex);
            return new VerificationServiceResult { Succeeded = false, Error = "Failed to send verification email." };
        }
    }

    public void SaveVerificationCode(SaveVerificationCodeRequest request)
    {
        _cache.Set(request.Email.ToLowerInvariant(), request.Code, request.ValidFor);
    }

    public VerificationServiceResult VerifyVerificationCode(VerifyVerificationCodeRequest request)
    {
        var key = request.Email.ToLowerInvariant();

        if(_cache.TryGetValue(key, out string? storedCode))
        {
            if (storedCode == request.Code)
            {
                _cache.Remove(key);
                return new VerificationServiceResult { Succeeded = true, Message = "Verification successful!"};
            }
        }
        return new VerificationServiceResult {  Succeeded = false, Error = "Invalid or expired verification code."};
    }
}
