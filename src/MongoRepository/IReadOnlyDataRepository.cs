using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MongoRepository
{
    /// <summary>	Interface for a read only data repository. </summary>
    /// <typeparam name="TEntity">	Type of the entity. </typeparam>
    /// <typeparam name="TKey">   	Type of the key. </typeparam>
    public interface IReadOnlyDataRepository<TEntity, in TKey> : IRepository
            where TEntity : class, IEntity<TKey>, new()
    {
        /// <summary>	Gets an entity using the given identifier. </summary>
        /// <param name="id">	The Identifier to use. </param>
        /// <returns>	A TEntity. </returns>
        Task<TEntity> Get(TKey id);

        /// <summary>	Gets all entities using the given identifiers. </summary>
        /// <param name="ids">	The Identifier to use. </param>
        /// <returns>	A TEntity. </returns>
        Task<IEnumerable<TEntity>> Get(IEnumerable<TKey> ids);

        /// <summary>	Gets all entities in this collection. </summary>
        /// <returns>
        ///     An enumerator that allows foreach to be used to process all items in this collection.
        /// </returns>
        Task<IEnumerable<TEntity>> GetAll();
    }
}
