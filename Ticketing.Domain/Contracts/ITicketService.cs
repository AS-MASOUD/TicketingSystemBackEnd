using Ticketing.Domain.Common;

namespace Ticketing.Domain.Contracts;

public interface ITicketService: IAsyncRepository<Entities.Ticket>
{
    Task<int> GetNextRowNumberAsync();
}