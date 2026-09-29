using System;

namespace MongoRepository
{
    /// <summary>
    /// Keeps a string property out of the trimming that
    /// <see cref="ReadWriteRepository{TEntity, TKey}"/> applies before <c>Add</c>,
    /// <c>AddRange</c> and <c>Update</c>.
    /// </summary>
    /// <remarks>
    /// Use it for values whose surrounding whitespace carries meaning or has to round-trip
    /// unchanged — natural keys held outside the id, hashes, signatures, preformatted text.
    /// The entity's key never needs it: the id is not trimmed in the first place.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class NoTrimAttribute : Attribute
    {
    }
}
