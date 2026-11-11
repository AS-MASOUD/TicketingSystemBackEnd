using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Domain.Contracts;
using Ticketing.Infrastructure.Database;

namespace Ticketing.Application.Service.Ticket
{
    public class TicketService : ITicketService
    {
        public TicketService(TicketingDbContext dbContext)
        {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public TicketingDbContext DbContext { get; }

        public async Task<Domain.Entities.Ticket> AddAsync(Domain.Entities.Ticket entity)
        {
            await DbContext.Tickets.AddAsync(entity);
            await DbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(Domain.Entities.Ticket entity)
        {
            DbContext.Tickets.Remove(entity);
            await DbContext.SaveChangesAsync();
            return true;
        }

        public async Task<Domain.Entities.Ticket> GetAsync(Expression<Func<Domain.Entities.Ticket, bool>> expression)
        {
            return await DbContext.Tickets.Where(expression).FirstOrDefaultAsync();
        }

        public async Task<List<Domain.Entities.Ticket>> ListAsync(Expression<Func<Domain.Entities.Ticket, bool>>? expression)
        {
            if (expression == null)
            {
                return await DbContext.Tickets.ToListAsync();
            }
            return await DbContext.Tickets.Where(expression).ToListAsync();
        }

        public async Task<int> GetNextRowNumberAsync()
        {
            var max = await DbContext.Tickets
                .AsNoTracking()
                .Select(t => (int?)t.TicketRowNumber)
                .MaxAsync();
            return (max ?? 0) + 1;
        }

        public async Task<Domain.Entities.Ticket> UpdateAsync(Domain.Entities.Ticket entity)
        {
            var result = DbContext.Tickets.Update(entity);
            await DbContext.SaveChangesAsync();
            return entity;
        }
    }
}
