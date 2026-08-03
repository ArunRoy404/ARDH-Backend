using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Models;

namespace CleanArchitecture.Infrastructure.Interface;

public interface IGenericRepository<T> where T : class
{
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task<bool> AnyAsync(Expression<Func<T, bool>> filter);
    Task<bool> AnyAsync();
    /// <summary>
    /// Checks for existence including soft-deleted records (ignores the IsDeleted query filter).
    /// Use for uniqueness checks so soft-deleted records still block reuse of unique values.
    /// </summary>
    Task<bool> AnyIncludingDeletedAsync(Expression<Func<T, bool>> filter);
    Task<int> CountAsync(Expression<Func<T, bool>> filter);
    Task<int> CountAsync();
    Task<T> GetByIdAsync(object id);
    Task<Pagination<TResult>> ToPagination<TResult>(
        int pageIndex,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        Expression<Func<T, object>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TResult>> selector = null);
    Task<T?> FirstOrDefaultAsync(
       Expression<Func<T, bool>> filter,
       Func<IQueryable<T>, IQueryable<T>>? include = null);
    Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> filter, Expression<Func<T, object>> sort, bool ascending = true);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);
    Task Delete(object id);
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null);
}
