using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data;
using System.Web;

namespace Data
{
    public class RepoBase<TContext, T, TRF> : IDisposable where TContext : DbContext  where T : RepoBase<TContext, T, TRF> where TRF : BaseRepositoriesFactory<TContext, T, TRF>
    {

        public bool AutoDetectChangesEnabled
        {
            set { Factory.Context.Configuration.AutoDetectChangesEnabled = value; }
        }
        protected int _userId;

        public int CurrentUserId => _userId;

        

        public async Task<string> SaveAsync(bool WithMessage = false, bool Catched = false)
        {
            if (_userId > 4)
            {
                Track();
            }
            // var entries = Parent.Context.ChangeTracker.Entries()
            //.Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            //.ToList();
            // foreach (var entry in entries)
            // {
            //     foreach (string o in entry.CurrentValues.PropertyNames)
            //     {
            //         var property = entry.Property(o);

            //         //var currentVal = property.CurrentValue == null ? "" : property.CurrentValue.ToString();
            //         //var originalVal = property.OriginalValue == null ? "" : property.OriginalValue.ToString();

            //         //if (currentVal != originalVal)
            //         //{
            //         //    if (entry.Entity.GetType().Name.Contains("PropetyPair"))
            //         //    {
            //         //        //  make and add log record
            //         //    }
            //         //}
            //     }
            // }

            string ret = "";
            try
            {
                await Factory.Context.SaveChangesAsync();
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    ret += string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        ret += string.Format("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);

                    }
                }
                if (!WithMessage)
                {
                    //Logger.Log.Error("DB update error: " + ret, e);
                    throw e;
                }
            }
            catch (Exception e)
            {
                var mess = e.InnerException != null ? (e.InnerException.InnerException?.Message ?? e.InnerException.Message) : e.Message;
                ret += mess;
                if (!WithMessage)
                {
                    //Logger.Log.Error("DB update error: " + ret, e);
                    throw e;
                }
            }

            return ret;
        }
        public string Save(bool WithMessage = false, bool Catched = false)
        {

            if (_userId > 4)
            {
                Track();
            }

            string ret = "";
            try
            {
                Factory.Context.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    ret += string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        ret += string.Format("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);

                    }
                }
                if (!WithMessage)
                {
                    //Logger.Log.Error("DB update error: " + ret, e);
                    throw e;
                }
            }
            catch (Exception e)
            {
                var mess = e.InnerException != null ? (e.InnerException.InnerException?.Message ?? e.InnerException.Message) : e.Message;

                ret += mess;
                if (!WithMessage)
                {
                    if (!Catched)
                    {
                        //Logger.Log.Error("DB update error: " + mess, e);
                    }
                    throw e;
                }
            }

            return ret;
        }

        public List<DbEntityEntry> Modified()
        {
            return Factory.Context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified)
                .ToList();
        }

        private void Track()
        {
            var added = Factory.Context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added)
                .ToList();
            foreach (var entry in added)
            {
                if (entry.Entity is ITrackedEntity)
                {
                    ((ITrackedEntity)entry.Entity).CreateDate = DateTime.Now;
                    ((ITrackedEntity)entry.Entity).LastEditorId = _userId;
                }
            }

            var modified = Factory.Context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified)
                .ToList();
            foreach (var entry in modified)
            {
                if (entry.Entity is ITrackedEntity)
                {

                    ((ITrackedEntity)entry.Entity).EditDate = DateTime.Now;
                    ((ITrackedEntity)entry.Entity).LastEditorId = _userId;

                    (Factory.Context.Entry(entry.Entity).Property("CreateDate")).IsModified = false;
                }
            }
        }

        //private bool _disposed = false;

        public TRF Factory;

        public RepoBase(int userId, TRF parent)
        {
            this._userId = userId;
            //this.Context = overrideContext;
            Factory = parent;


        }

        
        void Dispose(bool disposing)
        {

            if (disposing)
            {
                //Context.Dispose();
            }


            //this._disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        

    }
}
