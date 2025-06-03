using WebApi.Models;

namespace WebApi.Services
{
    public interface IVerificationService
    {
        void SaveVerificationCode(SaveVerificationCodeRequest request);
        Task<VerificationServiceResult> SendVerificationCodeAsync(SendVerificationCodeRequest request);
        VerificationServiceResult VerifyVerificationCode(VerifyVerificationCodeRequest request);
    }
}