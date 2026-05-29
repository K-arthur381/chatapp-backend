using ChatApp.Api.Interfaces;
using ChatApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);
        public async Task<IReadOnlyList<T>> ListAllAsync() => await _dbSet.AsNoTracking().ToListAsync();
        public async Task<T> AddAsync(T entity) { await _dbSet.AddAsync(entity); return entity; }
        public void Update(T entity) => _dbSet.Update(entity);
        public void Delete(T entity) => _dbSet.Remove(entity);
        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
