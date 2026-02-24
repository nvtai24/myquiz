using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Payments.Commands.CreatePaymentOrder;
using MyQuizGenerator.Application.Payments.Commands.HandleSepayWebhook;
using MyQuizGenerator.Application.Payments.DTOs;
using MyQuizGenerator.Application.Payments.Queries.GetUserSubscription;
using MyQuizGenerator.Infrastructure.Settings;

namespace MyQuizGenerator.Presentation.Controllers;

/// <summary>
/// Handles subscription payments and Sepay webhook.
/// </summary>
[Route("api/payments")]
public class PaymentsController : BaseApiController
{
    private readonly PaymentSettings _paymentSettings;

    public PaymentsController(IOptions<PaymentSettings> paymentSettings)
    {
        _paymentSettings = paymentSettings.Value;
    }

    /// <summary>
    /// Creates a payment order for a subscription plan.
    /// Returns bank transfer details for the user to complete payment.
    /// </summary>
    [Authorize]
    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreatePaymentOrderRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Invalid token");
        }

        var command = new CreatePaymentOrderCommand(userId, request);
        var result = await Mediator.Send(command);

        // Populate bank details from settings
        result.BankAccount = _paymentSettings.AccountNumber;
        result.AccountName = _paymentSettings.AccountName;
        result.BankCode = _paymentSettings.BankCode;
        result.QrCodeUrl = GenerateQRCodeUrl(result.Amount, result.OrderCode);

        return ApiCreated(result, "Payment order created. Please transfer the exact amount with the provided content.");
    }


    private string GenerateQRCodeUrl(decimal amount, string content)
    {
        // Using Sepay Quick Link
        // https://qr.sepay.vn/img?acc=SO_TAI_KHOAN&bank=NGAN_HANG&amount=SO_TIEN&des=NOI_DUNG&template=TEMPLATE
        var template = "compact2";
        var encodedContent = Uri.EscapeDataString(content);
        return $"{_paymentSettings.QrCodeUrl}?acc={_paymentSettings.AccountNumber}&bank={_paymentSettings.BankCode}&amount={amount}&des={encodedContent}&template={template}";
    }

    /// <summary>
    /// Sepay webhook endpoint to receive payment notifications.
    /// Authenticated via API token header.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("sepay-webhook")]
    public async Task<IActionResult> SepayWebhook(
        [FromHeader(Name = "Authorization")] string? authorization,
        [FromBody] SepayWebhookRequest request)
    {
        // Verify API token
        var expectedToken = $"Apikey {_paymentSettings.ApiToken}";
        if (string.IsNullOrEmpty(authorization) || authorization != expectedToken)
        {
            return Unauthorized(new { success = false, message = "Invalid API token" });
        }

        var command = new HandleSepayWebhookCommand(request);
        var result = await Mediator.Send(command);

        if (result)
        {
            return Ok(new { success = true });
        }

        return StatusCode(500, new { success = false, message = "Failed to process webhook" });
    }

    /// <summary>
    /// Gets the current user's active subscription information.
    /// </summary>
    [Authorize]
    [HttpGet("my-subscription")]
    public async Task<IActionResult> GetMySubscription()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Invalid token");
        }

        var query = new GetUserSubscriptionQuery(userId);
        var result = await Mediator.Send(query);

        if (result == null)
        {
            return ApiNotFound("No subscription found.");
        }

        return ApiOk(result, "Subscription retrieved successfully");
    }
}
