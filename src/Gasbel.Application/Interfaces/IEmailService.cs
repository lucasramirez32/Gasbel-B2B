using Gasbel.Domain.Entities;

namespace Gasbel.Application.Interfaces;

public record EmailDispatchResult(
    bool BusinessEmailSent,
    bool CustomerEmailSent,
    string? BusinessError = null,
    string? CustomerError = null,
    string? PreviewHtmlPath = null
);

public interface IEmailService
{
    Task<EmailDispatchResult> SendQuoteEmailsAsync(
        Quote quote,
        CommercialSettings settings,
        CancellationToken cancellationToken = default);
}
