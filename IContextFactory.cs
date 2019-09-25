using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataRepository
{

    public interface IContextFactory<out TContext>
        where TContext : DbContext
    {
        /// <summary>
        /// Creates new database context
        /// </summary>
        /// <returns>New Database context</returns>
        TContext Create(bool LazyLoadEnable = false);
    }

    

}
