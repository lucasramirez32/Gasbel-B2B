using Gasbel.Application.Interfaces;
using Gasbel.Domain.Entities;
using Gasbel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gasbel.Infrastructure.Repositories;

public class QuoteRepository : IQuoteRepository
{
    private readonly GasbelDbContext _dbContext;

    public QuoteRepository(GasbelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Quote> CreateQuoteAsync(Quote quote, CancellationToken cancellationToken = default)
    {
        _dbContext.Quotes.Add(quote);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return quote;
    }

    public async Task<Quote?> GetQuoteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Quotes
            .Include(q => q.Items)
            .ThenInclude(qi => qi.Product)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }
}
