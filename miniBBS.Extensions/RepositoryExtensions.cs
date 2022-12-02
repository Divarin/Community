using miniBBS.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Extensions
{
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Sometimes only one record is expected to exist for a given query. 
        /// If more than one exists then all but the most recent is deleted 
        /// and only the most recent is returned
        /// </summary>
        public static T PruneAllButMostRecent<T>(this IEnumerable<T> set, IDependencyResolver di)
            where T : class, IDataModel
        {
            var repo = di.GetRepository<T>();
            return PruneAllButMostRecent(set, repo);
        }

        /// <summary>
        /// Sometimes only one record is expected to exist for a given query. 
        /// If more than one exists then all but the most recent is deleted 
        /// and only the most recent is returned
        /// </summary>
        public static T PruneAllButMostRecent<T>(this IEnumerable<T> set, IRepository<T> repo)
            where T : class, IDataModel
        {
            if (true != set?.Any() || set.Count() == 1)
                return set?.FirstOrDefault();

            var maxId = set.Max(x => x.Id);
            var result = set.Single(x => x.Id == maxId);

            var toBeDeleted = set
                .Where(x => x.Id != maxId)
                .ToList();

            repo.DeleteRange(toBeDeleted);

            return result;
        }
    }
}
