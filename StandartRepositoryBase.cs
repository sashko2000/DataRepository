using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq.Expressions;
using System.Threading.Tasks;


namespace Data
{
    public abstract class StandartRepositoryBase<T, TContext, TRepo, TRf> : StandartRepo<T, TContext, TRepo, TRf> where T : class where TContext : DbContext where TRepo : RepoBase<TContext, TRepo, TRf> where TRf : BaseRepositoriesFactory<TContext, TRepo, TRf>
    {
        public sealed override T Update(T entity, params object[] anyParameters)
        {
            return base.Update(entity, anyParameters);
        }


        public StandartRepositoryBase(int userId, TRepo overrideContext)
        {
            _userId = userId;
            Parent = overrideContext;

        }

        public sealed override Task<T> UpdateAsync(T entity, bool anyParameter = false)
        {
            return base.UpdateAsync(entity, anyParameter);
        }

        public sealed override T Remove(T entity)
        {
            return base.Remove(entity);
        }

        public sealed override bool Remove(Expression<Func<T, bool>> @where)
        {
            return base.Remove(@where);
        }

        public sealed override T Remove(int id)
        {
            return base.Remove(id);
        }

        public sealed override Task<T> RemoveAsync(T entity)
        {
            return base.RemoveAsync(entity);
        }

        public sealed override Task<T> RemoveAsync(int id)
        {
            return base.RemoveAsync(id);
        }

        
    }
}

