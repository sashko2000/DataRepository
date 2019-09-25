using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Transactions;
using DataRepository;
using eLuxr.Helpers;



namespace Data
{
    public abstract class StandartRepo<T, TContext, TRepo, TRf> where T : class where TContext : DbContext where TRepo : RepoBase<TContext, TRepo, TRf> where TRf : BaseRepositoriesFactory<TContext, TRepo, TRf>
    {
        public TRepo Parent { get; set; }
        protected TContext Context => Parent.Factory.Context;
        protected DbSet<T> EntitySet => Context.Set<T>();
        protected string ParentName;

        protected int _userId;

        protected bool? _admin = null;

        protected bool? _segment_admin = null;

        public abstract IQueryable<T> GetAllElementsForEdit(Expression<Func<T, bool>> @where = null);

        public virtual bool BeforeUpdate(T entity, params object[] anyParameters)
        {
            return true;
        }

        public virtual bool AfterUpdate(T entity, params object[] anyParameters)
        {
            return true;
        }
        public virtual T Update(T entity, params object[] anyParameters)
        {

            //Context.Entry(entity).CurrentValues.SetValues(entity);
            //Context.Entry(entity).State = EntityState.Modified;
            //return entity;

            var query = GetAllElementsForEdit();
            var keyDict = EntityKeyHelper.Instance.GetKeysWithNames(entity, Context);
            var i = 0;
            foreach (var key in keyDict)
            {
                query = query.Where(key.Key + " = @" + i, key.Value);
                i++;
            }
            var trackedEntity = query.FirstOrDefault();

            if (query.Any())
            {

                BeforeUpdate(entity, anyParameters);
                Context.Entry(trackedEntity).CurrentValues.SetValues(entity);
                Context.Entry(trackedEntity).State = EntityState.Modified;
                
                AfterUpdate(entity, anyParameters);
                return trackedEntity;
            }
            return null;
        }

        public virtual T FindEntity(T entity)
        {
            
            var query = GetAllElementsForEdit();
            var keyDict = EntityKeyHelper.Instance.GetKeysWithNames(entity, Context);
            var i = 0;
            foreach (var key in keyDict)
            {
                query = query.Where(key.Key + " = @" + i, key.Value);
                i++;
            }
            var trackedEntity = query.FirstOrDefault();

            if (query.Any())
            {
                Context.Entry(trackedEntity).CurrentValues.SetValues(entity);

                return trackedEntity;
            }
            return null;
        }

        protected TRf Repositories
        {
            get { return Parent.Factory; }
        }

        public abstract bool Admin { get; }
        public abstract bool SegmentAdmin { get; }

        public virtual void ExecuteCommand(string CommandText, params object[] Parameters)
        {
            using (var transactionScope = new TransactionScope())
            {
                // some stuff in dbcontext

                Context.Database.ExecuteSqlCommand(CommandText, Parameters);

                Context.SaveChanges();
                transactionScope.Complete();
            }
        }

        public virtual IQueryable<T> SqlQuery(string q, params object[] parameters)
        {
            return EntitySet.SqlQuery(q, parameters).AsQueryable();
        }

        

        public virtual void BulkInsert(IEnumerable<T> entities)
        {
            using (var transactionScope = new TransactionScope())
            {
                // some stuff in dbcontext

                Context.BulkInsert(entities);

                Context.SaveChanges();
                transactionScope.Complete();
            }
        }

        public virtual void LoadReference(T entity, Expression<Func<T, Object>> property)
        {
            Context.Entry(entity).Reference(property).Load();
        }

        public virtual Task LoadReferenceAsync(T entity, Expression<Func<T, Object>> property)
        {
            return Context.Entry(entity).Reference(property).LoadAsync();
        }

        public virtual void LoadCollection<TElement>(T entity, Expression<Func<T, ICollection<TElement>>> property) where TElement : class
        {
            if (Context.Entry(entity).State == EntityState.Detached)
            {
                EntitySet.Attach(entity);
            }
            //if (order != null)
            //{
            //    Context.Entry(entity).Collection(property).Query().OrderBy(order).Load();
            //}
            //else
            {
                Context.Entry(entity).Collection(property).Load();
            }
            
        }
                
        public virtual Task LoadCollectionAsync<TElement>(T entity, Expression<Func<T, ICollection<TElement>>> property/*, string order = null*/) where TElement : class
        {
            if (Context.Entry(entity).State == EntityState.Detached)
            {
                EntitySet.Attach(entity);
            }
            //if (order != null)
            //{
            //    return Context.Entry(entity).Collection(property).Query().OrderBy(order).LoadAsync();
            //}
            //else
            {
                return Context.Entry(entity).Collection(property).LoadAsync();
            }

        }

        public StandartRepo()
        {

        }

        


        public virtual T Create()
        {
            return EntitySet.Create();
        }

        public virtual int GetCount(Expression<Func<T, bool>> @where = null)
        {
            return GetAllElementsForEdit(where).Count();
            
        }


        public virtual (IQueryable<T> query, int count) GetElements(int startIndex, int count, string sorting, string filter, Expression<Func<T, bool>> @where = null)
        {
           
            IQueryable<T> query = GetAllElementsForEdit(where);
            //if (where == null)
            //{
            //    query = EntitySet.AsQueryable();
            //}
            //else
            //{
            //    query = EntitySet.Where(where).AsQueryable();
            //}

            int allCount = query.Count();

            if (!string.IsNullOrEmpty(sorting))
            {
                query = query.OrderBy(sorting);

            }

            if (!string.IsNullOrEmpty(filter))
            {
                //query = query.Where(filter);
                query = query.FilterQuery(filter);

            }

            if (!string.IsNullOrEmpty(sorting))
            {
                return (count > 0 ? query.Skip(startIndex).Take(count) : query, allCount);
            }
            else
            {
                return (query, allCount);
            }
        }


        public virtual IQueryable<T> GetAllElementsForLookups(Expression<Func<T, bool>> @where = null)
        {
            IQueryable<T> query;
            if (where == null)
            {
                query = EntitySet.AsQueryable();
            }
            else
            {
                query = EntitySet.Where(where).AsQueryable();
            }
            return query;
        }


        public virtual IQueryable<T> GetAllElementsUnsafe(Expression<Func<T, bool>> @where = null)
        {
            IQueryable<T> query;
            if (where == null)
            {
                query = EntitySet.AsQueryable();
            }
            else
            {
                query = EntitySet.Where(where).AsQueryable();
            }
            return query;
        }

        public virtual T Insert(T entity)
        {
            try
            {
                var ent = EntitySet.Add(entity);
                return ent;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }


        }


        public virtual async Task<T> UpdateAsync(T entity, bool anyParameter = false)
        {

            var query = GetAllElementsForEdit();
            var keyDict = EntityKeyHelper.Instance.GetKeysWithNames(entity, Context);

            foreach (var key in keyDict)
            {
                query = query.Where(key.Key + " = @0", key.Value);
            }
            var trackedEntity = await query.FirstOrDefaultAsync();

            if (query.Any())
            {
                
                Context.Entry(trackedEntity).CurrentValues.SetValues(entity);
                Context.Entry(trackedEntity).State = EntityState.Modified;

                BeforeUpdate(trackedEntity, anyParameter);

                return trackedEntity;
            }
            return null;
        }



        public virtual bool BeforeRemove(T entity)
        {
            return true;
        }

        //public virtual void AfterInsert(T entity)
        //{
            
        //}

        public virtual T Remove(T entity)
        {

            var query = GetAllElementsForEdit();
            var keyDict = EntityKeyHelper.Instance.GetKeysWithNames(entity, Context);

            foreach (var key in keyDict)
            {
                query = query.Where(key.Key + " = @0", key.Value);
            }
            if (query.Any())
            {
                BeforeRemove(entity);
                return EntitySet.Remove(entity);
            }
            else
            {
                throw new AccessViolationException("You do not have a rights");
            }

        }

        public virtual async Task<T> RemoveAsync(T entity)
        {

            var query = GetAllElementsForEdit();
            var keyDict = EntityKeyHelper.Instance.GetKeysWithNames(entity, Context);

            foreach (var key in keyDict)
            {
                query = query.Where(key.Key + " = @0", key.Value);
            }
            if (await query.AnyAsync())
            {
                BeforeRemove(entity);
                return EntitySet.Remove(entity);
            }
            else
            {
                throw new AccessViolationException("You do not have a rights");
            }
        }

        public virtual T Remove(int id)
        {

            var entity = GetAllElementsForEdit().Where("Id=" + id).FirstOrDefault();
            if (entity == null)
            {
                throw new AccessViolationException("You do not have a rights");
            }
            BeforeRemove(entity);
            return EntitySet.Remove(entity);
        }

        public virtual async Task<T> RemoveAsync(int id)
        {

            var entity = await GetAllElementsForEdit().Where("Id=" + id).FirstOrDefaultAsync();
            if (entity == null)
            {
                throw new AccessViolationException("You do not have a rights");
            }
            BeforeRemove(entity);
            return EntitySet.Remove(entity);
        }
        
        public virtual bool Remove(Expression<Func<T, bool>> @where)
        {

            var objects = EntitySet.Where(where).AsQueryable();
            //var allSuccess = true;
            foreach (var obj in objects)
            {
                Remove(obj);

            }
            return true;
        }

        //public virtual void SaveChanges()
        //{
        //    var added = Context.ChangeTracker.Entries()
        //        .Where(e => e.State == EntityState.Added)
        //        .ToList();
        //    foreach (var entry in added)
        //    {
        //        if (entry.Entity is LightBaseModel)
        //        {
        //            ((Data.LightBaseModel)entry.Entity).CreateDate = DateTime.Now;
        //            ((Data.LightBaseModel)entry.Entity).LastEditorId = _userId;
        //        }
        //    }

        //    var modified = Context.ChangeTracker.Entries()
        //        .Where(e => e.State == EntityState.Modified)
        //        .ToList();
        //    foreach (var entry in modified)
        //    {
        //        if (entry.Entity is LightBaseModel)
        //        {
        //            ((Data.LightBaseModel)entry.Entity).EditDate = DateTime.Now;
        //            ((Data.LightBaseModel)entry.Entity).LastEditorId = _userId;
        //        }
        //    }

        //    try
        //    {
        //        Context.SaveChanges();
        //    }

        //    catch (DbEntityValidationException e)
        //    {
        //        foreach (var eve in e.EntityValidationErrors)
        //        {
        //            Debug.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
        //                eve.Entry.Entity.GetType().Name, eve.Entry.State);
        //            foreach (var ve in eve.ValidationErrors)
        //            {
        //                Debug.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
        //                    ve.PropertyName, ve.ErrorMessage);
        //            }
        //        }
        //        throw;
        //    }
        //    catch (Exception e)
        //    {
        //        throw;
        //    }
        //}

        public virtual T Get(Expression<Func<T, bool>> @where, params Expression<Func<T, Object>>[] include)
        {
            if (include != null)
            {
                var result = GetAllElementsForLookups().AsQueryable();
                foreach (var inc in include)
                {
                    result = result.Include(inc);
                }

                return result.Where(where).FirstOrDefault();
            }
            else
            {
                return GetAllElementsForLookups().Where(where).FirstOrDefault();


            }

        }

        public virtual Task<T> GetAsync(Expression<Func<T, bool>> @where, params Expression<Func<T, Object>>[] include)
        {
            if (include != null)
            {
                var result = GetAllElementsForLookups().AsQueryable();
                foreach (var inc in include)
                {
                    result = result.Include(inc);
                }

                return result.Where(where).FirstOrDefaultAsync();
            }
            else
            {
                return GetAllElementsForLookups().Where(where).FirstOrDefaultAsync();


            }

        }

        public virtual T GetOriginal(Expression<Func<T, bool>> @where, params Expression<Func<T, Object>>[] include)
        {
            if (include != null)
            {
                var result = GetAllElementsForEdit().AsNoTracking().AsQueryable();
                foreach (var inc in include)
                {
                    result = result.Include(inc);
                }

                return result.Where(where).FirstOrDefault();
            }
            else
            {
                return GetAllElementsForEdit().AsNoTracking().Where(where).FirstOrDefault();


            }
            


        }

        public virtual T GetForEdit(Expression<Func<T, bool>> @where, params Expression<Func<T, Object>>[] include)
        {
            if (include != null)
            {
                var result = GetAllElementsForEdit().AsQueryable();
                foreach (var inc in include)
                {
                    result = result.Include(inc);
                }

                return result.Where(where).FirstOrDefault();
            }
            else
            {
                return GetAllElementsForEdit().Where(where).FirstOrDefault();


            }

        }

        public virtual T GetUnsafe(Expression<Func<T, bool>> @where, params Expression<Func<T, Object>>[] include)
        {
            if (include != null)
            {
                var result = GetAllElementsUnsafe().AsQueryable();
                foreach (var inc in include)
                {
                    result = result.Include(inc);
                }

                return result.Where(where).FirstOrDefault();
            }
            else
            {
                return GetAllElementsUnsafe().Where(where).FirstOrDefault();
            }

        }

        public virtual T GetUnsafeAsNoTracking(Expression<Func<T, bool>> @where, params Expression<Func<T, Object>>[] include)
        {
            if (include != null)
            {
                var result = GetAllElementsUnsafe().AsQueryable().AsNoTracking();
                foreach (var inc in include)
                {
                    result = result.Include(inc);
                }

                return result.Where(where).FirstOrDefault();
            }
            else
            {
                return GetAllElementsUnsafe().Where(where).AsNoTracking().FirstOrDefault();
            }

        }

        public virtual Task<T> GetUnsafeAsync(Expression<Func<T, bool>> @where, params Expression<Func<T, Object>>[] include)
        {
            if (include != null)
            {
                var result = GetAllElementsUnsafe().AsQueryable();
                foreach (var inc in include)
                {
                    result = result.Include(inc);
                }

                return result.Where(where).FirstOrDefaultAsync();
            }
            else
            {
                return GetAllElementsUnsafe().Where(where).FirstOrDefaultAsync();
            }

        }

        public virtual T Get(int id, params Expression<Func<T, object>>[] include)
        {

            if (include != null)
            {
                var result = GetAllElementsForLookups().AsQueryable();
                foreach (var inc in include)
                {
                    result = result.Include(inc);
                }
                try
                {
                    return result.Where("Id=" + id).FirstOrDefault();
                }
                catch (Exception e)
                {

                    throw;
                }

            }
            else
            {
                return GetAllElementsForLookups().Where("Id=" + id).FirstOrDefault();
            }

        }

        public virtual Task<T> GetAsync(int id, params Expression<Func<T, Object>>[] include)
        {

            if (include != null)
            {
                var result = GetAllElementsForLookups().AsQueryable();
                foreach (var inc in include)
                {
                    result = result.Include(inc);
                }

                return result.Where("Id=" + id).FirstOrDefaultAsync();


            }
            else
            {
                return GetAllElementsForLookups().Where("Id=" + id).FirstOrDefaultAsync();
            }

        }

        //public virtual Task<T> GetAsyncOptimize(int id, params Expression<Func<T, Object>>[] include)
        //{

        //    if (include != null)
        //    {
        //        var result = GetAllElementsForLookups().AsQueryable();
        //        foreach (var inc in include)
        //        {
        //            result = result.IncludeOptimized(inc);
        //        }

        //        return result.Where("Id=" + id).FirstOrDefaultAsync();


        //    }
        //    else
        //    {
        //        return GetAllElementsForLookups().Where("Id=" + id).FirstOrDefaultAsync();
        //    }

        //}

        public virtual T GetForEdit(int id, params Expression<Func<T, Object>>[] include)
        {

            if (include != null)
            {
                var result = GetAllElementsForEdit().AsQueryable();
                foreach (var inc in include)
                {
                    result = result.Include(inc);
                }

                return result.Where("Id=" + id).FirstOrDefault();


            }
            else
            {
                return GetAllElementsForEdit().Where("Id=" + id).FirstOrDefault();
            }

        }

        public virtual T GetUnsafe(int id, params Expression<Func<T, Object>>[] include)
        {

            if (include != null)
            {
                var result = GetAllElementsUnsafe().AsQueryable();
                foreach (var inc in include)
                {
                    result = result.Include(inc);
                }

                return result.Where("Id=" + id).FirstOrDefault();


            }
            else
            {
                return GetAllElementsUnsafe().Where("Id=" + id).FirstOrDefault();
            }

        }

        public virtual T GetUnsafeAsNoTracking(int id, params Expression<Func<T, Object>>[] include)
        {

            if (include != null)
            {
                var result = GetAllElementsUnsafe().AsQueryable();
                foreach (var inc in include)
                {
                    result = result.Include(inc);
                }

                return result.Where("Id=" + id).AsNoTracking().FirstOrDefault();


            }
            else
            {
                return GetAllElementsUnsafe().Where("Id=" + id).AsNoTracking().FirstOrDefault();
            }

        }

        public virtual Task<T> GetUnsafeAsync(int id, params Expression<Func<T, Object>>[] include)
        {

            if (include != null)
            {
                var result = GetAllElementsUnsafe().AsQueryable();
                foreach (var inc in include)
                {
                    result = result.Include(inc);
                }

                return result.Where("Id=" + id).FirstOrDefaultAsync();


            }
            else
            {
                return GetAllElementsUnsafe().Where("Id=" + id).FirstOrDefaultAsync();
            }

        }

       
        public virtual IQueryable<T> GetQuerableById(int id)
        {
            return GetAllElementsForLookups().Where("Id = @0", id);
        }


        public virtual bool HaveRightsForView(int id)
        {
            return GetAllElementsForLookups().Where("Id=" + id).Any();
        }
        public virtual bool HaveRightsForView(Expression<Func<T, bool>> @where)
        {
            return GetAllElementsForLookups().Where(@where).Any();

        }

        public virtual bool HaveRightsForEdit(int? id)
        {
            if (id == null)
            {
                return false;
            }
            return GetAllElementsForEdit().Where("Id=" + id.Value).Any();
        }

        public virtual bool HaveRightsForEdit(Expression<Func<T, bool>> @where)
        {
            return GetAllElementsForEdit().Where(@where).Any();

        }

        public virtual IQueryable<T> GetQuerableByIdUnsafe(int id)
        {
            return EntitySet.Where("Id = @0", id);
        }

        

    }
}