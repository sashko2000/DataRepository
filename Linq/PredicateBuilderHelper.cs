using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace eLuxr.Helpers
{
    /// <summary>
    /// Enables the efficient, dynamic composition of query predicates.
    /// </summary>
    public static class PredicateBuilderHelper
    {
        /// <summary>
        /// Creates a predicate that evaluates to true.
        /// </summary>
        public static Expression<Func<T, bool>> True<T>() { return param => true; }

        /// <summary>
        /// Creates a predicate that evaluates to false.
        /// </summary>
        public static Expression<Func<T, bool>> False<T>() { return param => false; }

        /// <summary>
        /// Creates a predicate expression from the specified lambda expression.
        /// </summary>
        public static Expression<Func<T, bool>> Create<T>(Expression<Func<T, bool>> predicate) { return predicate; }

        /// <summary>
        /// Combines the first predicate with the second using the logical "and".
        /// </summary>
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.AndAlso);
        }

        /// <summary>
        /// Combines the first predicate with the second using the logical "or".
        /// </summary>
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.OrElse);
        }

        /// <summary>
        /// Negates the predicate.
        /// </summary>
        public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression)
        {
            var negated = Expression.Not(expression.Body);
            return Expression.Lambda<Func<T, bool>>(negated, expression.Parameters);
        }

        /// <summary>
        /// Combines the first expression with the second using the specified merge function.
        /// </summary>
        static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            // zip parameters (map from parameters of second to parameters of first)
            var map = first.Parameters
                .Select((f, i) => new { f, s = second.Parameters[i] })
                .ToDictionary(p => p.s, p => p.f);

            // replace parameters in the second lambda expression with the parameters in the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // create a merged lambda expression with parameters from the first expression
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        class ParameterRebinder : ExpressionVisitor
        {
            readonly Dictionary<ParameterExpression, ParameterExpression> map;

            ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
            {
                this.map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
            }

            public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
            {
                return new ParameterRebinder(map).Visit(exp);
            }

            protected override Expression VisitParameter(ParameterExpression p)
            {
                ParameterExpression replacement;

                if (map.TryGetValue(p, out replacement))
                {
                    p = replacement;
                }

                return base.VisitParameter(p);
            }
        }

        public static IQueryable<TEntity> WhereFieldStartWith<TEntity, TProperty>(this IQueryable<TEntity> query, string field, TProperty value)
        {
            var param = Expression.Parameter(typeof(TEntity));

            

            Type entityType = typeof(TEntity);
            var propertyInfo = entityType.GetProperties()
                                    .FirstOrDefault(p => p.Name == field);
            if (propertyInfo == null)
            {
                return query;
            }


            MemberExpression m = Expression.MakeMemberAccess(param, propertyInfo);
            
            ConstantExpression c;
            
            Expression call;
            if (propertyInfo.PropertyType.Name == "Boolean")
            {
                
                bool val = false;

                if (!bool.TryParse(value.ToString().ToLower(), out val))
                {
                    int intval;
                    if (int.TryParse(value.ToString(), out intval))
                    {
                        val = intval > 0;
                    }
                }
                c = Expression.Constant(val, typeof(bool));
                call = Expression.Equal(m, c);
            }
            else
            {
                c = Expression.Constant(value, typeof(TProperty));
                var mi = typeof (string).GetMethod("StartsWith", new Type[] {typeof (string)});
                call = Expression.Call(m, mi, c);
            }
            
            Expression<Func<TEntity, bool>> lambda = Expression.Lambda<Func<TEntity, bool>>(call, param);

            return query.Where(lambda);
        }

        public static IQueryable<TEntity> TryOrderBy<TEntity, TProperty>(this IQueryable<TEntity> query, string field)
        {
            //var param = Expression.Parameter(typeof(TEntity));



            Type entityType = typeof(TEntity);
            var propertyInfo = entityType.GetProperties()
                                    .FirstOrDefault(p => p.Name == field);
            if (propertyInfo == null)
            {
                return query;
            }


            var param = Expression.Parameter(typeof(TEntity), "x");
            Expression conversion = Expression.Convert(Expression.Property(param, field), typeof(object));   //important to use the Expression.Convert
            var lambda = Expression.Lambda<Func<TEntity, object>>(conversion, param);

            //MemberExpression m = Expression.MakeMemberAccess(param, propertyInfo);
            //MethodInfo mi = typeof(string).GetMethod("StartsWith", new Type[] { typeof(string) });
            //Expression call = Expression.Call(m, mi, c);
            //Expression<Func<TEntity, bool>> lambda = Expression.Lambda<Func<TEntity, bool>>(call, param);

            return query.OrderBy(lambda);
        }


        public static IEnumerable<TEntity> WhereFieldStartWith<TEntity, TProperty>(this IEnumerable<TEntity> query, string field, TProperty value)
        {
            var param = Expression.Parameter(typeof(TEntity));



            Type entityType = typeof(TEntity);
            var propertyInfo = entityType.GetProperties()
                                    .FirstOrDefault(p => p.Name == field);
            if (propertyInfo == null)
            {
                return query;
            }


            MemberExpression m = Expression.MakeMemberAccess(param, propertyInfo);
            ConstantExpression c = Expression.Constant(value, typeof(TProperty));
            MethodInfo mi = typeof(string).GetMethod("StartsWith", new Type[] { typeof(string) });
            Expression call = Expression.Call(m, mi, c);
            Expression<Func<TEntity, bool>> lambda = Expression.Lambda<Func<TEntity, bool>>(call, param);

            return query.AsQueryable().Where(lambda);
        }



        public static IQueryable<TEntity> FilterQuery<TEntity>(this IQueryable<TEntity> query, string jtFilter)
        {
            var filterarray = jtFilter.Split(';');
            foreach (var item in filterarray)
            {
                var farray = item.Split('=');
                query = query.WhereFieldStartWith(farray[0], farray[1]);
            }
            return query;
        }

        

        public static IEnumerable<TEntity> FilterQuery<TEntity>(this IEnumerable<TEntity> query, string jtFilter)
        {
            var filterarray = jtFilter.Split(';');
            foreach (var item in filterarray)
            {
                var farray = item.Split('=');
                query = query.WhereFieldStartWith(farray[0], farray[1]);
            }
            return query;
        }

        

        


    }

}
