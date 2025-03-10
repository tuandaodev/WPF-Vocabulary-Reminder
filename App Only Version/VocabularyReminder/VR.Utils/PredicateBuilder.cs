﻿using System;
using System.Linq.Expressions;

namespace FAI.Core.Utilities.Linq
{
    public enum PredicateOperator
    {
        Or,
        And
    }

    public static class PredicateBuilder
    {
        private class RebindParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            public RebindParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node == _oldParameter)
                    return _newParameter;

                return base.VisitParameter(node);
            }
        }

        public static ExpressionStarter<T> New<T>(Expression<Func<T, bool>> expr = null) { return new ExpressionStarter<T>(expr); }

        public static ExpressionStarter<T> New<T>(bool defaultExpression) { return new ExpressionStarter<T>(defaultExpression); }

        [Obsolete("Use PredicateBuilder.New() instead.")]
        public static Expression<Func<T, bool>> True<T>() { return new ExpressionStarter<T>(true); }

        [Obsolete("Use PredicateBuilder.New() instead.")]
        public static Expression<Func<T, bool>> False<T>() { return new ExpressionStarter<T>(false); }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var expr2Body = new RebindParameterVisitor(expr2.Parameters[0], expr1.Parameters[0]).Visit(expr2.Body);
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, expr2Body), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var expr2Body = new RebindParameterVisitor(expr2.Parameters[0], expr1.Parameters[0]).Visit(expr2.Body);
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, expr2Body), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> Extend<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second, PredicateOperator @operator = PredicateOperator.Or)
        {
            return @operator == PredicateOperator.Or ? first.Or(second) : first.And(second);
        }

        public static Expression<Func<T, bool>> Extend<T>(this ExpressionStarter<T> first, Expression<Func<T, bool>> second, PredicateOperator @operator = PredicateOperator.Or)
        {
            return @operator == PredicateOperator.Or ? first.Or(second) : first.And(second);
        }
    }
}
