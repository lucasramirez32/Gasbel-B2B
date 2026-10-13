using Gasbel.Domain.Entities;

namespace Gasbel.Application.Interfaces;

public interface IQuoteRepository
{
    Task<Quote> CreateQuoteAsync(Quote quote, CancellationToken cancellationToken = default);
    Task<Quote?> GetQuoteByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
