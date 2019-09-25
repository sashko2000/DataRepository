using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data;

namespace DataRepository
{

    public interface IRepositoryFactory<TContext,T, TRF>
        where TContext : DbContext where T : RepoBase<TContext, T, TRF> where TRF : BaseRepositoriesFactory<TContext, T, TRF>
    {
        
        T Create(int UserId, BaseRepositoriesFactory<TContext, T, TRF> rep);
    }

    

}
