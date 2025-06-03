using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VerificationController(IVerificationService verificationService, IHttpClientFactory httpClientFactory) : ControllerBase
{
    private readonly IVerificationService _verificationService = verificationService;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    [HttpPost("send")]
    public async Task<IActionResult> Send(SendVerificationCodeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new {Error = "Recipient email address is required"});
        }

        var result = await _verificationService.SendVerificationCodeAsync(request);
        return result.Succeeded
            ? Ok(result)
            : StatusCode(500, result);
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify(VerifyVerificationCodeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { Error = "Invalid or expired code." });
        }

        var result = _verificationService.VerifyVerificationCode(request);

        if (result.Succeeded)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var confirmEmail = new { email = request.Email };
            var authServiceUrl = "https://example.com/api/confirm-email"; // Replace with your AuthService URL
            var response = await httpClient.PostAsJsonAsync(authServiceUrl, confirmEmail);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(500, new { Error = "Verification succeeded but failed to confirm email in AuthService." });
            }

            return Ok(result);
        }
        else
        {
            return StatusCode(500, result);
        }
    }

    
}
