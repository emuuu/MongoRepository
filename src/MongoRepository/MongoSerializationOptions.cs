using System;
using System.Collections.Generic;

namespace MongoRepository
{
    /// <summary>
    /// Selects which MongoDB serialization conventions the library registers, and which types
    /// they apply to. Every switch is off by default, so an application that does not set any
    /// of them keeps the driver's out-of-the-box behaviour.
    /// </summary>
    /// <remarks>
    /// Conventions are applied while the driver builds the class map for a type. A class map is
    /// built once per type and per process, on first use, and is never revisited — so the
    /// conventions have to be registered before the first document of that type is read or
    /// written. See <see cref="MongoRepositoryConventions"/> for the two ways to register them.
    /// </remarks>
    public class MongoSerializationOptions
    {
        /// <summary>   The convention pack name used when <see cref="ConventionPackName"/> is empty. </summary>
        public const string DefaultConventionPackName = "MongoGenericRepository";

        /// <summary>
        /// Whether a stored document may carry fields the entity class does not declare.
        /// </summary>
        /// <value>
        /// <c>null</c>, the default, registers nothing: the driver stays strict and an unknown
        /// field makes deserialization throw a <see cref="FormatException"/>. <c>true</c> ignores
        /// unknown fields, which is what keeps documents readable during a rolling update, while
        /// instances running a newer schema already write fields the older ones do not know.
        /// <c>false</c> registers strict behaviour explicitly.
        /// </value>
        public bool? IgnoreExtraElements { get; set; }

        /// <summary>
        /// Whether properties that are <c>null</c> are left out of the stored document.
        /// </summary>
        /// <value>
        /// <c>null</c>, the default, registers nothing: a null property is stored as BSON null.
        /// <c>true</c> omits it, which changes the shape of the stored document — an
        /// <c>$exists</c> filter and a sparse or partial index then see a field that is absent
        /// rather than one that is null. <c>false</c> registers the storing of nulls explicitly.
        /// </value>
        public bool? IgnoreIfNull { get; set; }

        /// <summary>
        /// The namespaces the conventions are limited to. Initialised so that the configuration
        /// binder fills it in place; setting it to <c>null</c> is treated as an empty list.
        /// </summary>
        /// <value>
        /// An empty list, the default, applies the conventions to every type. Otherwise a type
        /// matches when its namespace equals an entry, or starts with that entry followed by a
        /// dot — <c>MyApp.Orders</c> therefore covers <c>MyApp.Orders.Archive</c> but not
        /// <c>MyApp.OrdersArchive</c>. Types without a namespace only match while the list is
        /// empty. Entries that are null, empty or whitespace are dropped; a list that holds
        /// nothing usable is rejected rather than quietly covering every type.
        /// </value>
        public IList<string> Namespaces { get; set; } = new List<string>();

        /// <summary>
        /// An additional predicate that narrows the conventions to the types it accepts.
        /// </summary>
        /// <value>
        /// <c>null</c>, the default, applies no additional narrowing. A predicate is combined
        /// with <see cref="Namespaces"/> by AND: a type has to satisfy both. This can only be
        /// set from code — the configuration binder skips delegate properties.
        /// </value>
        /// <remarks>
        /// The driver calls the predicate while it holds its own registry lock, so it has to be
        /// side-effect free: it must not register or unregister conventions itself, and it must
        /// not block on anything that does.
        /// </remarks>
        public Func<Type, bool> TypeFilter { get; set; }

        /// <summary>
        /// The name the convention pack is registered under in the driver's convention registry.
        /// </summary>
        /// <value>
        /// Defaults to <see cref="DefaultConventionPackName"/>. Change it only to keep the pack
        /// apart from another one an application registers itself; registering twice under the
        /// same name replaces the earlier pack. The driver's own names, <c>__defaults__</c> and
        /// <c>__attributes__</c>, are rejected — taking one of them over would strip conventions
        /// the whole process depends on. Pick a name nothing else registers under: the driver
        /// keeps every pack sharing a name and deletes all of them together, so a collision means
        /// both packs apply and both go when this one is unregistered.
        /// </value>
        public string ConventionPackName { get; set; } = DefaultConventionPackName;
    }
}
