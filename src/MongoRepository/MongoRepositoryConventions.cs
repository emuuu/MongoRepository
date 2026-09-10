using MongoDB.Bson.Serialization.Conventions;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MongoRepository
{
    /// <summary>
    /// Registers the serialization conventions described by <see cref="MongoSerializationOptions"/>
    /// with the MongoDB driver.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The driver's convention registry is process-wide state. Registration is therefore not
    /// scoped to a repository, a context or a dependency injection container: it affects every
    /// class map built afterwards, in the whole process.
    /// </para>
    /// <para>
    /// There are two ways in. Calling <see cref="Register(MongoDbOptions)"/> explicitly always
    /// applies the options passed to it, and is the recommended way — place it early in
    /// application start-up, before the first repository call. Failing that, the first
    /// <see cref="EntityContext{TEntity}"/> constructed with options that ask for a convention
    /// registers them as a fallback — a context whose switches are all unset leaves the state
    /// open for a later one. Once something is registered, later <see cref="MongoDbOptions"/>
    /// instances carrying different values do not change it.
    /// </para>
    /// </remarks>
    public static class MongoRepositoryConventions
    {
        /// <summary>
        /// Guards the whole state machine: the registry entry, the pack name and the mark have
        /// to move together, or a second thread can observe a mark whose pack is not in the
        /// registry yet and map a class without it.
        /// </summary>
        private static readonly object _gate = new object();

        /// <summary>   The name the driver registers its own default conventions under. </summary>
        private const string DriverDefaultsPackName = "__defaults__";

        /// <summary>   The name the driver registers its attribute conventions under. </summary>
        private const string DriverAttributesPackName = "__attributes__";

        private static int _registered;

        private static string _registeredPackName;

        /// <summary>
        /// Registers the conventions described by <paramref name="options"/>.<see cref="MongoDbOptions.Serialization"/>.
        /// </summary>
        /// <param name="options">   The mongoDB connection options carrying the serialization options. </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="options"/> or its <see cref="MongoDbOptions.Serialization"/>
        /// is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown for an unusable <see cref="MongoSerializationOptions.Namespaces"/> list or a
        /// <see cref="MongoSerializationOptions.ConventionPackName"/> that collides with one of the
        /// driver's own pack names.
        /// </exception>
        public static void Register(MongoDbOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            Register(options.Serialization);
        }

        /// <summary>
        /// Registers the conventions described by <paramref name="options"/>, replacing a pack this
        /// library registered earlier. Does nothing when neither
        /// <see cref="MongoSerializationOptions.IgnoreExtraElements"/> nor
        /// <see cref="MongoSerializationOptions.IgnoreIfNull"/> is set.
        /// </summary>
        /// <param name="options">   The serialization options to apply. </param>
        /// <exception cref="ArgumentNullException"> Thrown when <paramref name="options"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <see cref="MongoSerializationOptions.Namespaces"/> holds entries but none of
        /// them is usable, which would otherwise widen the conventions to every type, or when
        /// <see cref="MongoSerializationOptions.ConventionPackName"/> collides with one of the
        /// driver's own pack names. Neither case touches the registry, so a rejected call leaves
        /// an existing registration intact.
        /// </exception>
        /// <remarks>
        /// Only class maps built after this call see the conventions. Types that have already
        /// been read or written keep the behaviour they were mapped with, which is why this
        /// belongs at the start of application start-up. Registering for the first time never
        /// leaves the registry without a pack, but replacing a live registration does so for the
        /// moment between removing the old pack and adding the new one — the driver offers no
        /// atomic swap. A type mapped in that moment gets neither. Re-registering while requests
        /// are in flight is therefore not supported.
        /// </remarks>
        public static void Register(MongoSerializationOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (!HasConventions(options))
                return;

            lock (_gate)
            {
                Apply(options);

                // Set last: until the pack is in the registry, no thread may take the shortcut
                // in EnsureRegistered.
                Interlocked.Exchange(ref _registered, 1);
            }
        }

        /// <summary>
        /// Removes the registered convention pack and allows a later registration to take effect
        /// again.
        /// </summary>
        /// <remarks>
        /// Only a pack this library registered itself is removed; with nothing registered the call
        /// does nothing, because the driver deletes every entry under a name and a pack someone
        /// else put there is not this library's to take. Registering under a different name drops
        /// the earlier pack, so there is never more than one of this library's packs in the
        /// registry. Class maps that were already built are unaffected: the conventions have
        /// been baked into them and removing the pack does not undo that. Intended for tests and
        /// for hosts that reconfigure themselves, not for switching behaviour at runtime.
        /// </remarks>
        public static void Unregister()
        {
            lock (_gate)
            {
                // Cleared first: a thread that reads the mark while the pack is being removed
                // must fall through to the lock and get its own registration afterwards, rather
                // than take the shortcut and map a class with no pack at all.
                Interlocked.Exchange(ref _registered, 0);

                if (_registeredPackName == null)
                    return;

                // ConventionRegistry.Remove deletes every entry under a name, so this only ever
                // runs for a name this library put there itself.
                ConventionRegistry.Remove(_registeredPackName);

                _registeredPackName = null;
            }
        }

        /// <summary>
        /// Registers the conventions carried by <paramref name="options"/> unless a registration
        /// has already happened. Called by <see cref="EntityContext{TEntity}"/> so applications
        /// that never call <see cref="Register(MongoDbOptions)"/> still get their configured
        /// conventions.
        /// </summary>
        /// <param name="options">   The mongoDB connection options, or <c>null</c>. </param>
        /// <exception cref="ArgumentException">
        /// Thrown for options the library rejects, which surfaces from the
        /// <see cref="EntityContext{TEntity}"/> constructor. Options that simply ask for nothing
        /// are not an error and are skipped.
        /// </exception>
        internal static void EnsureRegistered(MongoDbOptions options)
        {
            // Fast path for every context after the first one: a plain read, no lock.
            if (Volatile.Read(ref _registered) != 0)
                return;

            var serialization = options?.Serialization;

            if (serialization == null || !HasConventions(serialization))
                return;

            lock (_gate)
            {
                if (_registered != 0)
                    return;

                Apply(serialization);

                Interlocked.Exchange(ref _registered, 1);
            }
        }

        /// <summary>   Whether the options ask for any convention at all. </summary>
        private static bool HasConventions(MongoSerializationOptions options)
        {
            return options.IgnoreExtraElements.HasValue || options.IgnoreIfNull.HasValue;
        }

        /// <summary>   Builds the convention pack and puts it into the driver's registry. </summary>
        private static void Apply(MongoSerializationOptions options)
        {
            var packName = ResolvePackName(options.ConventionPackName);

            var pack = new ConventionPack();

            if (options.IgnoreExtraElements.HasValue)
                pack.Add(new IgnoreExtraElementsConvention(options.IgnoreExtraElements.Value));

            if (options.IgnoreIfNull.HasValue)
                pack.Add(new IgnoreIfNullConvention(options.IgnoreIfNull.Value));

            // Everything that can reject the options runs before the registry is touched, so a
            // rejected call leaves the previous registration intact.
            var typeFilter = BuildTypeFilter(options);

            // A rename would otherwise leave the earlier pack behind, still applying to types
            // Unregister no longer knows about.
            if (_registeredPackName != null && _registeredPackName != packName)
                ConventionRegistry.Remove(_registeredPackName);

            // Registering the same name twice would leave both packs in the registry. Skipping
            // this when nothing of ours is registered keeps the common case — one registration
            // during start-up — free of a window in which no pack is in the registry.
            if (_registeredPackName == packName)
                ConventionRegistry.Remove(packName);

            ConventionRegistry.Register(packName, pack, typeFilter);

            _registeredPackName = packName;
        }

        /// <summary>
        /// Resolves the name the pack is registered under and rejects the two names the driver
        /// keeps for itself.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown for the driver's own marker names. Registering under one of them would replace
        /// the driver's default or attribute pack, which strips conventions the whole process
        /// depends on — including the one that makes attributes such as <c>[BsonId]</c> work.
        /// </exception>
        private static string ResolvePackName(string conventionPackName)
        {
            if (string.IsNullOrEmpty(conventionPackName))
                return MongoSerializationOptions.DefaultConventionPackName;

            if (conventionPackName == DriverDefaultsPackName || conventionPackName == DriverAttributesPackName)
                throw new ArgumentException(
                    "ConventionPackName must not be one of the driver's own pack names, \"" + DriverDefaultsPackName
                        + "\" and \"" + DriverAttributesPackName + "\".",
                    nameof(MongoSerializationOptions.ConventionPackName));

            return conventionPackName;
        }

        /// <summary>
        /// Turns the namespace list and the type filter into the single predicate the driver's
        /// registry takes. Both narrow the set of types, so they are combined by AND.
        /// </summary>
        private static Func<Type, bool> BuildTypeFilter(MongoSerializationOptions options)
        {
            var namespaces = SnapshotNamespaces(options.Namespaces);
            var typeFilter = options.TypeFilter;

            if (namespaces.Length == 0)
                return typeFilter ?? (type => true);

            if (typeFilter == null)
                return type => MatchesNamespace(type, namespaces);

            return type => MatchesNamespace(type, namespaces) && typeFilter(type);
        }

        /// <summary>
        /// Copies the configured namespaces, so that editing the options afterwards cannot change
        /// what an already registered pack applies to. Entries that are null, empty or whitespace
        /// are dropped, and surrounding whitespace is trimmed.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when the list holds entries but none of them is usable. Silently falling back to
        /// an empty snapshot would widen the conventions to every type in the process, which is
        /// the opposite of what the list was set for.
        /// </exception>
        private static string[] SnapshotNamespaces(IList<string> namespaces)
        {
            if (namespaces == null || namespaces.Count == 0)
                return Array.Empty<string>();

            var snapshot = new List<string>(namespaces.Count);

            for (var i = 0; i < namespaces.Count; i++)
            {
                var candidate = namespaces[i];

                if (!string.IsNullOrWhiteSpace(candidate))
                    snapshot.Add(candidate.Trim());
            }

            if (snapshot.Count == 0)
                throw new ArgumentException(
                    "Namespaces holds no usable entry. Remove the list to apply the conventions to every type.",
                    nameof(MongoSerializationOptions.Namespaces));

            return snapshot.ToArray();
        }

        /// <summary>
        /// Whether the type sits in one of the configured namespaces. Matching is by whole
        /// segment: an entry covers the namespace itself and everything nested below it, so
        /// "MyApp.Orders" matches "MyApp.Orders.Archive" but not "MyApp.OrdersArchive".
        /// </summary>
        private static bool MatchesNamespace(Type type, string[] namespaces)
        {
            var typeNamespace = type.Namespace;

            if (string.IsNullOrEmpty(typeNamespace))
                return false;

            for (var i = 0; i < namespaces.Length; i++)
            {
                var candidate = namespaces[i];

                if (typeNamespace.Length == candidate.Length)
                {
                    if (string.Equals(typeNamespace, candidate, StringComparison.Ordinal))
                        return true;
                }
                else if (typeNamespace.Length > candidate.Length
                    && typeNamespace[candidate.Length] == '.'
                    && string.CompareOrdinal(typeNamespace, 0, candidate, 0, candidate.Length) == 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
