using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace miniBBS.Core.Helpers
{
    public class LambdaComparer<T, Tv> : IEqualityComparer<T>
        where Tv : IEquatable<Tv>
    {
        private readonly Func<T, Tv> _propertySelector;

        public LambdaComparer(Expression<Func<T, Tv>> propertySelector)
        {
            this._propertySelector = propertySelector.Compile();
        }

        public bool Equals(T x, T y)
        {
            var xVal = this._propertySelector(x);
            var yVal = this._propertySelector(y);
            return xVal.Equals(yVal);
        }

        public int GetHashCode(T obj)
        {
            var val = this._propertySelector(obj);
            return val.GetHashCode();
        }
    }
}
