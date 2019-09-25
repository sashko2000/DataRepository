using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataRepository;

namespace Data
{
    public abstract class BaseRepositoriesFactory<TContext, TRepo, TRf> : IDisposable where TContext : DbContext where TRepo : RepoBase<TContext, TRepo, TRf> where TRf : BaseRepositoriesFactory<TContext, TRepo, TRf>
    {
        public const string AnonymousUserName = "Anonymous";
        public const int AnonymousUserId = 1;
        public const string SystemUserName = "System";
        public const int SystemUserId = 2;
        public const string DiamondInventoryParsingWorkerUserName = "DiamondInventoryParsingWorker";
        public const int DiamondInventoryParsingWorkerUserId = 3;
        protected bool LazyLoadEnabled;

        public TContext Context;

        private Dictionary<int, TRepo> UserRepositories;

        private readonly IContextFactory<TContext> _dbContextFactory;
        private readonly IRepositoryFactory<TContext, TRepo, TRf> _dbRepositoryFactory;

        public BaseRepositoriesFactory(IContextFactory<TContext> dbContextFactory, IRepositoryFactory<TContext, TRepo, TRf> dbRepositoryFactory, bool lazyLoadEnabled = false)
        {
            LazyLoadEnabled = lazyLoadEnabled;

            _dbContextFactory = dbContextFactory;
            _dbRepositoryFactory = dbRepositoryFactory;
            Context = dbContextFactory.Create(lazyLoadEnabled);

            UserRepositories = new Dictionary<int, TRepo>();
        }

        private static readonly object padlock = new object();

        public void ReleaseAllContexts()
        {
            if (UserRepositories != null && UserRepositories.Any())
            {
                foreach (var userRepo in UserRepositories)
                {
                    userRepo.Value?.Dispose();
                }
                UserRepositories.Clear();
            }
            Context.Dispose();

        }

        private Object thisLock = new Object();

        public TRepo GetByUser(int userId)
        {
            TRepo rep;
            lock (padlock)
            {
                
                //bool inDictionary;

                if (UserRepositories == null)
                {
                    UserRepositories = new Dictionary<int, TRepo>();
                }

                if (UserRepositories.ContainsKey(userId))
                {
                    rep = UserRepositories[userId];
                    if (rep == null)
                    {
                        rep = _dbRepositoryFactory.Create(userId, this);
                    }
                }
                else
                {
                    rep = _dbRepositoryFactory.Create(userId, this);
                    UserRepositories.Add(userId, rep);
                }


                //try
                //{
                //    inDictionary = UserRepositories.TryGetValue(userId, out rep);
                //}
                //catch (Exception)
                //{
                //    throw new Exception("");
                //}

                //if (rep == null)
                //{
                //    rep = _dbRepositoryFactory.Create(userId, this);
                //}

                //if (!inDictionary)
                //{
                //    UserRepositories.Add(userId, rep);
                //}
            }
            return rep;
            
        }

        private TRepo _current;
        public TRepo Current
        {
            get
            {
                if (_current != null)
                {
                    return _current;
                }
                var userId = GetUserId();


                return _current = GetByUser(userId);
            }
        }

        public abstract int GetUserId();

        public abstract int GetSegment();


        private TRepo _system;
        public TRepo System
        {
            get
            {
                if (_system == null)
                {
                    return _system = GetByUser(SystemUserId);
                }
                else
                {
                    return _system;
                }
            }
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseAllContexts();

            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private DbContextTransaction dbContextTransaction;
        public void BeginTransaction()
        {
            dbContextTransaction = Context.Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            dbContextTransaction.Commit();
        }

        public void RollbackTransaction()
        {
            dbContextTransaction.Rollback();
        }

        public void ReNew()
        {
            ReleaseAllContexts();
            Context = _dbContextFactory.Create(LazyLoadEnabled);
        }
    }
}