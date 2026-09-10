using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoRepository.Tests.ConventionScopes.Attributed;
using MongoRepository.Tests.ConventionScopes.Combined;
using MongoRepository.Tests.ConventionScopes.Excluded;
using MongoRepository.Tests.ConventionScopes.Fallback;
using MongoRepository.Tests.ConventionScopes.Filtered;
using MongoRepository.Tests.ConventionScopes.General;
using MongoRepository.Tests.ConventionScopes.Included;
using MongoRepository.Tests.ConventionScopes.Lifecycle;
using MongoRepository.Tests.ConventionScopes.Orders;
using MongoRepository.Tests.ConventionScopes.Orders.Nested;
using MongoRepository.Tests.ConventionScopes.OrdersArchive;
using MongoRepository.Tests.ConventionScopes.Outside;
using MongoRepository.Tests.Infrastructure;

namespace MongoRepository.Tests
{
    /// <summary>
    /// Covers the opt-in serialization conventions and, just as importantly, the limits the
    /// driver puts on them. Conventions live in a process-wide registry and are baked into a
    /// class map the first time a type is serialised, so both the scope of a registration and
    /// the moment it happens are observable behaviour.
    /// </summary>
    /// <remarks>
    /// Every test uses entity types of its own. The driver caches class maps for the lifetime of
    /// the process and offers no way to drop them, so a type that has been mapped once can never
    /// again be used to observe a different registration. Sharing the fixtures from
    /// <c>Infrastructure/TestEntities.cs</c> would break for the same reason.
    /// </remarks>
    [Collection("MongoDB")]
    public class SerializationConventionsTests : IAsyncLifetime
    {
        private const string DatabaseName = "ConventionTestDb";

        /// <summary> Field written by a hypothetical newer schema that the entity classes here do not declare. </summary>
        private const string UnknownField = "AddedByNewerSchema";

        private readonly MongoDbFixture _fixture;
        private readonly IMongoDatabase _rawDatabase;

        public SerializationConventionsTests(MongoDbFixture fixture)
        {
            _fixture = fixture;
            _rawDatabase = new MongoClient(fixture.ConnectionString).GetDatabase(DatabaseName);
        }

        public ValueTask InitializeAsync() => default;

        public ValueTask DisposeAsync()
        {
            // The registry is process-wide. A registration left behind would leak into every
            // test that runs after this one, including the ones in other classes.
            MongoRepositoryConventions.Unregister();
            return default;
        }

        // --- Registering at all ---

        [Fact]
        public async Task WithoutRegistration_UnknownField_Throws()
        {
            // Guards the premise of every tolerance test below: the driver is strict by default.
            await AssertStrict<StrictByDefaultItem>();
        }

        [Fact]
        public async Task IgnoreExtraElements_UnknownFieldIsIgnored()
        {
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true
            });

            await AssertTolerant<TolerantItem>();
        }

        [Fact]
        public async Task IgnoreExtraElementsFalse_KeepsDriverStrict()
        {
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = false
            });

            await AssertStrict<ExplicitlyStrictItem>();
        }

        [Fact]
        public async Task Register_FromMongoDbOptions_AppliesSerializationSection()
        {
            MongoRepositoryConventions.Register(new MongoDbOptions
            {
                ReadOnlyConnection = _fixture.ConnectionString,
                ReadWriteConnection = _fixture.ConnectionString,
                Serialization = new MongoSerializationOptions { IgnoreExtraElements = true }
            });

            await AssertTolerant<OptionsOverloadItem>();
        }

        [Fact]
        public async Task Register_UnderCustomPackName_AppliesAndIsRemovedAgain()
        {
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true,
                ConventionPackName = "CustomPackName"
            });

            await AssertTolerant<CustomPackItem>();

            MongoRepositoryConventions.Unregister();

            await AssertStrict<AfterCustomPackItem>();
        }

        [Fact]
        public void Register_AddsBothConventionsToTheRegistry()
        {
            // Counting is the only way to tell a registered "false" apart from no registration:
            // both leave the driver at its strict, null-storing default, and the driver's own
            // default pack already carries an IgnoreExtraElementsConvention of its own.
            var extraBefore = CountConventions<IgnoreExtraElementsConvention>(typeof(RegistryProbeItem));
            var nullBefore = CountConventions<IgnoreIfNullConvention>(typeof(RegistryProbeItem));

            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = false,
                IgnoreIfNull = false
            });

            Assert.Equal(extraBefore + 1, CountConventions<IgnoreExtraElementsConvention>(typeof(RegistryProbeItem)));
            Assert.Equal(nullBefore + 1, CountConventions<IgnoreIfNullConvention>(typeof(RegistryProbeItem)));

            MongoRepositoryConventions.Unregister();

            Assert.Equal(extraBefore, CountConventions<IgnoreExtraElementsConvention>(typeof(RegistryProbeItem)));
            Assert.Equal(nullBefore, CountConventions<IgnoreIfNullConvention>(typeof(RegistryProbeItem)));
        }

        [Fact]
        public void Register_WithOneSwitch_AddsOnlyThatConvention()
        {
            var extraBefore = CountConventions<IgnoreExtraElementsConvention>(typeof(SingleSwitchProbeItem));
            var nullBefore = CountConventions<IgnoreIfNullConvention>(typeof(SingleSwitchProbeItem));

            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreIfNull = false
            });

            Assert.Equal(extraBefore, CountConventions<IgnoreExtraElementsConvention>(typeof(SingleSwitchProbeItem)));
            Assert.Equal(nullBefore + 1, CountConventions<IgnoreIfNullConvention>(typeof(SingleSwitchProbeItem)));
        }

        [Fact]
        public void Namespaces_KeepTheConventionsOutOfTheRegistryForOtherTypes()
        {
            var includedBefore = CountConventions<IgnoreExtraElementsConvention>(typeof(IncludedItem));
            var excludedBefore = CountConventions<IgnoreExtraElementsConvention>(typeof(ExcludedItem));

            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true,
                Namespaces = { "MongoRepository.Tests.ConventionScopes.Included" }
            });

            Assert.Equal(includedBefore + 1, CountConventions<IgnoreExtraElementsConvention>(typeof(IncludedItem)));
            Assert.Equal(excludedBefore, CountConventions<IgnoreExtraElementsConvention>(typeof(ExcludedItem)));
        }

        [Fact]
        public async Task Register_WithUnusableNamespaces_ThrowsAndRegistersNothing()
        {
            var options = new MongoSerializationOptions
            {
                IgnoreExtraElements = true,
                Namespaces = { "   " }
            };

            var baseline = CountConventions<IgnoreExtraElementsConvention>(typeof(AfterUnusableNamespacesItem));

            // Falling back to "every type" here would widen the conventions instead of narrowing
            // them, which is the opposite of what the list was set for.
            Assert.Throws<ArgumentException>(() => MongoRepositoryConventions.Register(options));

            // Nothing reached the registry on the way out.
            Assert.Equal(baseline, CountConventions<IgnoreExtraElementsConvention>(typeof(AfterUnusableNamespacesItem)));

            // The failed call must not leave a half-registered state behind.
            BuildContext<AfterUnusableNamespacesItem>(new MongoSerializationOptions
            {
                IgnoreExtraElements = true
            });

            await AssertTolerant<AfterUnusableNamespacesItem>();
        }

        [Fact]
        public void Register_WithReservedPackName_Throws()
        {
            // Taking over one of the driver's own pack names would strip the conventions the
            // whole process depends on, including the ones that make [BsonId] work.
            foreach (var reserved in new[] { "__defaults__", "__attributes__" })
            {
                var options = new MongoSerializationOptions
                {
                    IgnoreExtraElements = true,
                    ConventionPackName = reserved
                };

                Assert.Throws<ArgumentException>(() => MongoRepositoryConventions.Register(options));
            }

            // The driver's own packs are still in place.
            Assert.NotEmpty(ConventionRegistry.Lookup(typeof(ReservedNameProbeItem)).Conventions);
        }

        [Fact]
        public void RejectedRegistration_LeavesAnExistingOneIntact()
        {
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true
            });

            var registered = CountConventions<IgnoreExtraElementsConvention>(typeof(RollbackProbeItem));

            // Rejected while the previous registration is live: the options are validated before
            // the registry is touched, so nothing may be lost.
            Assert.Throws<ArgumentException>(() => MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true,
                Namespaces = { "   " }
            }));

            Assert.Equal(registered, CountConventions<IgnoreExtraElementsConvention>(typeof(RollbackProbeItem)));
        }

        [Fact]
        public void Register_UnderANewName_DropsThePreviousPack()
        {
            var baseline = CountConventions<IgnoreExtraElementsConvention>(typeof(RenameProbeItem));

            // The first pack covers every type.
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true,
                ConventionPackName = "FirstPackName"
            });

            Assert.Equal(baseline + 1, CountConventions<IgnoreExtraElementsConvention>(typeof(RenameProbeItem)));

            // The second one is scoped elsewhere, which is what makes the two distinguishable:
            // an implementation that ignored this call would leave the first pack applying here.
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true,
                ConventionPackName = "SecondPackName",
                Namespaces = { "MongoRepository.Tests.ConventionScopes.Included" }
            });

            Assert.Equal(baseline, CountConventions<IgnoreExtraElementsConvention>(typeof(RenameProbeItem)));
            Assert.Equal(
                CountConventions<IgnoreExtraElementsConvention>(typeof(IncludedItem)),
                baseline + 1);

            MongoRepositoryConventions.Unregister();

            // Neither pack may resurface.
            Assert.Equal(baseline, CountConventions<IgnoreExtraElementsConvention>(typeof(RenameProbeItem)));
            Assert.Equal(baseline, CountConventions<IgnoreExtraElementsConvention>(typeof(IncludedItem)));
        }

        [Fact]
        public void Unregister_WithoutRegistration_LeavesAPackOfTheSameNameAlone()
        {
            // Someone else's pack, sitting under the name this library would use. The driver
            // deletes every entry under a name at once, so removing that name blindly would take
            // this one with it — a pack this library never registered.
            ConventionRegistry.Register(
                MongoSerializationOptions.DefaultConventionPackName,
                new ConventionPack { new IgnoreExtraElementsConvention(true) },
                type => type == typeof(UntouchedProbeItem));

            try
            {
                var withForeignPack = CountConventions<IgnoreExtraElementsConvention>(typeof(UntouchedProbeItem));

                MongoRepositoryConventions.Unregister();

                Assert.Equal(withForeignPack, CountConventions<IgnoreExtraElementsConvention>(typeof(UntouchedProbeItem)));
            }
            finally
            {
                ConventionRegistry.Remove(MongoSerializationOptions.DefaultConventionPackName);
            }
        }

        [Fact]
        public void ConcurrentRegistrations_LeaveExactlyOnePack()
        {
            var baseline = CountConventions<IgnoreExtraElementsConvention>(typeof(ConcurrencyProbeItem));

            Parallel.For(0, 32, _ => MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true
            }));

            // Registering without removing first, or removing without registering, would show up
            // here as a count other than one.
            Assert.Equal(baseline + 1, CountConventions<IgnoreExtraElementsConvention>(typeof(ConcurrencyProbeItem)));
        }

        [Fact]
        public void ConcurrentContexts_RegisterOnlyOnce()
        {
            var baseline = CountConventions<IgnoreExtraElementsConvention>(typeof(ConcurrentContextProbeItem));

            Parallel.For(0, 32, _ => BuildContext<ConcurrentContextProbeItem>(new MongoSerializationOptions
            {
                IgnoreExtraElements = true
            }));

            Assert.Equal(baseline + 1, CountConventions<IgnoreExtraElementsConvention>(typeof(ConcurrentContextProbeItem)));
        }

        // --- When the registration happens ---

        [Fact]
        public async Task ClassMapBuiltBeforeRegistration_StaysStrict()
        {
            // Building the class map up front is what a repository call before start-up
            // registration does; the conventions arrive too late to change it.
            BsonClassMap.LookupClassMap(typeof(EarlyMappedItem));

            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true
            });

            await AssertStrict<EarlyMappedItem>();
        }

        [Fact]
        public async Task Unregister_LeavesExistingClassMapsAlone()
        {
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true
            });

            await AssertTolerant<BeforeUnregisterItem>();

            MongoRepositoryConventions.Unregister();

            // Types mapped while the pack was registered keep the behaviour they were mapped
            // with; only types mapped from here on are strict again.
            await AssertTolerant<BeforeUnregisterItem>();
            await AssertStrict<AfterUnregisterItem>();
        }

        // --- Attributes against conventions ---

        [Fact]
        public async Task BsonIgnoreExtraElementsFalse_OverridesToleranceConvention()
        {
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true
            });

            // Control: the convention does reach types that do not carry the attribute, so
            // the strict result below cannot be explained by a registration that never took.
            await AssertTolerant<AttributeFreeControlItem>();

            // The registry appends attribute packs last, so the attribute wins.
            await AssertStrict<AttributeStrictItem>();
        }

        [Fact]
        public async Task BsonIgnoreExtraElements_OverridesStrictConvention()
        {
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = false
            });

            await AssertTolerant<AttributeTolerantItem>();
        }

        // --- IgnoreIfNull changes the stored document ---

        [Fact]
        public async Task WithoutIgnoreIfNull_NullPropertyIsStoredAsNull()
        {
            await Typed<NullStoredItem>().InsertOneAsync(
                new NullStoredItem { Id = "1" },
                cancellationToken: TestContext.Current.CancellationToken);

            var stored = await ReadRaw<NullStoredItem>("1");

            Assert.True(stored.Contains(nameof(NullStoredItem.Name)));
            Assert.Equal(BsonNull.Value, stored[nameof(NullStoredItem.Name)]);
        }

        [Fact]
        public async Task IgnoreIfNull_NullPropertyIsAbsentFromTheDocument()
        {
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreIfNull = true
            });

            await Typed<NullOmittedItem>().InsertOneAsync(
                new NullOmittedItem { Id = "1" },
                cancellationToken: TestContext.Current.CancellationToken);

            var stored = await ReadRaw<NullOmittedItem>("1");

            // The field is missing, not null — which is what $exists filters and sparse or
            // partial indexes react to.
            Assert.False(stored.Contains(nameof(NullOmittedItem.Name)));
        }

        [Fact]
        public async Task IgnoreIfNull_IsIndependentOfIgnoreExtraElements()
        {
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreIfNull = true
            });

            // Only the second switch was set, so unknown fields still fail.
            await AssertStrict<NullOmittedStrictItem>();
        }

        // --- Narrowing the scope ---

        [Fact]
        public async Task Namespaces_ApplyOnlyToListedNamespaces()
        {
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true,
                Namespaces = { "MongoRepository.Tests.ConventionScopes.Included" }
            });

            await AssertTolerant<IncludedItem>();
            await AssertStrict<ExcludedItem>();
        }

        [Fact]
        public async Task Namespaces_MatchWholeSegmentsOnly()
        {
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true,
                Namespaces = { "MongoRepository.Tests.ConventionScopes.Orders" }
            });

            await AssertTolerant<ScopedOrder>();
            await AssertTolerant<NestedOrder>();

            // "...OrdersArchive" starts with the entry as a string, but not as a namespace.
            await AssertStrict<ArchivedOrder>();
        }

        [Fact]
        public async Task TypeFilter_AppliesOnlyToAcceptedTypes()
        {
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true,
                TypeFilter = type => type == typeof(FilterAcceptedItem)
            });

            await AssertTolerant<FilterAcceptedItem>();
            await AssertStrict<FilterRejectedItem>();
        }

        [Fact]
        public async Task NamespacesAndTypeFilter_BothHaveToMatch()
        {
            MongoRepositoryConventions.Register(new MongoSerializationOptions
            {
                IgnoreExtraElements = true,
                Namespaces = { "MongoRepository.Tests.ConventionScopes.Combined" },
                TypeFilter = type => type.Name.StartsWith("Accepted", StringComparison.Ordinal)
            });

            await AssertTolerant<AcceptedInNamespaceItem>();

            // In the namespace, rejected by the filter.
            await AssertStrict<RejectedInNamespaceItem>();

            // Accepted by the filter, outside the namespace.
            await AssertStrict<AcceptedOutsideNamespaceItem>();
        }

        // --- The fallback in the EntityContext constructor ---

        [Fact]
        public async Task Fallback_FirstContextRegistersItsOwnOptions()
        {
            BuildContext<FallbackFirstItem>(new MongoSerializationOptions { IgnoreExtraElements = true });

            await AssertTolerant<FallbackFirstItem>();
        }

        [Fact]
        public async Task Fallback_LaterContextDoesNotChangeTheRegistration()
        {
            BuildContext<FallbackFirstOfTwoItem>(new MongoSerializationOptions { IgnoreExtraElements = true });

            // A second context carrying the opposite value arrives after the fact and is ignored.
            BuildContext<FallbackSecondOfTwoItem>(new MongoSerializationOptions { IgnoreExtraElements = false });

            await AssertTolerant<FallbackSecondOfTwoItem>();
        }

        [Fact]
        public async Task Fallback_ContextWithoutConventionsLeavesRegistrationOpen()
        {
            // Default options ask for nothing, so nothing is registered and no mark is set.
            BuildContext<FallbackNeutralItem>(new MongoSerializationOptions());

            BuildContext<FallbackLateItem>(new MongoSerializationOptions { IgnoreExtraElements = true });

            await AssertTolerant<FallbackLateItem>();
        }

        [Fact]
        public async Task Register_WithoutConventions_LeavesRegistrationOpen()
        {
            MongoRepositoryConventions.Register(new MongoSerializationOptions());

            BuildContext<FallbackAfterNoOpItem>(new MongoSerializationOptions { IgnoreExtraElements = true });

            await AssertTolerant<FallbackAfterNoOpItem>();
        }

        // --- Helpers ---

        /// <summary> The typed collection, reached the way an application reaches it. Building it maps the class. </summary>
        private IMongoCollection<TEntity> Typed<TEntity>() where TEntity : class
            => new EntityContext<TEntity>(_fixture.CreateOptions()).Collection(false);

        /// <summary> Constructs a context so its fallback registration runs, without mapping the class. </summary>
        private void BuildContext<TEntity>(MongoSerializationOptions serialization) where TEntity : class
        {
            var options = Options.Create(new MongoDbOptions
            {
                ReadOnlyConnection = _fixture.ConnectionString,
                ReadWriteConnection = _fixture.ConnectionString,
                Serialization = serialization
            });

            _ = new EntityContext<TEntity>(options);
        }

        /// <summary>
        /// Writes a document through the BSON collection, carrying a field the entity class does
        /// not declare. Going around the entity mapping is the only way to produce that shape,
        /// and it leaves the class map unbuilt.
        /// </summary>
        private Task SeedDocumentWithUnknownField<TEntity>()
            => _rawDatabase.GetCollection<BsonDocument>(typeof(TEntity).Name)
                .InsertOneAsync(new BsonDocument
                {
                    { "_id", ObjectId.GenerateNewId().ToString() },
                    { "Name", "seeded" },
                    { UnknownField, 1 }
                }, cancellationToken: TestContext.Current.CancellationToken);

        private async Task<BsonDocument> ReadRaw<TEntity>(string id)
            => await _rawDatabase.GetCollection<BsonDocument>(typeof(TEntity).Name)
                .Find(new BsonDocument("_id", id))
                .FirstAsync(TestContext.Current.CancellationToken);

        private Task<TEntity> ReadBack<TEntity>() where TEntity : class
            => Typed<TEntity>().Find(FilterDefinition<TEntity>.Empty).FirstAsync(TestContext.Current.CancellationToken);

        /// <summary>
        /// How many conventions of the given kind the registry hands out for a type. The driver
        /// ships some of these in its own default pack, so only the difference across a
        /// registration says anything.
        /// </summary>
        private static int CountConventions<TConvention>(Type type) where TConvention : IConvention
        {
            var count = 0;

            foreach (var convention in ConventionRegistry.Lookup(type).Conventions)
            {
                if (convention is TConvention)
                    count++;
            }

            return count;
        }

        /// <summary> Asserts the unknown field is ignored, so the document can still be read. </summary>
        private async Task AssertTolerant<TEntity>() where TEntity : class
        {
            await SeedDocumentWithUnknownField<TEntity>();

            var entity = await ReadBack<TEntity>();

            Assert.NotNull(entity);
        }

        /// <summary> Asserts the unknown field still makes deserialization fail. </summary>
        private async Task AssertStrict<TEntity>() where TEntity : class
        {
            await SeedDocumentWithUnknownField<TEntity>();

            var exception = await Assert.ThrowsAsync<FormatException>(() => ReadBack<TEntity>());

            Assert.Contains(UnknownField, exception.Message);
        }
    }
}

namespace MongoRepository.Tests.ConventionScopes.General
{
    [EntityDatabase("ConventionTestDb")]
    public class StrictByDefaultItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class TolerantItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class ExplicitlyStrictItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class OptionsOverloadItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class CustomPackItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class AfterCustomPackItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class EarlyMappedItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class NullStoredItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class NullOmittedItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class NullOmittedStrictItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    /// <summary> Only ever looked up in the convention registry, never mapped or stored. </summary>
    public class RegistryProbeItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    /// <summary> Only ever looked up in the convention registry, never mapped or stored. </summary>
    public class SingleSwitchProbeItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class AfterUnusableNamespacesItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    /// <summary> Only ever looked up in the convention registry, never mapped or stored. </summary>
    public class ReservedNameProbeItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    /// <summary> Only ever looked up in the convention registry, never mapped or stored. </summary>
    public class RollbackProbeItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    /// <summary> Only ever looked up in the convention registry, never mapped or stored. </summary>
    public class RenameProbeItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    /// <summary> Only ever looked up in the convention registry, never mapped or stored. </summary>
    public class UntouchedProbeItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    /// <summary> Only ever looked up in the convention registry, never mapped or stored. </summary>
    public class ConcurrencyProbeItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class ConcurrentContextProbeItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }
}

namespace MongoRepository.Tests.ConventionScopes.Attributed
{
    /// <summary> Declares strictness on the type while a tolerant convention is registered. </summary>
    [EntityDatabase("ConventionTestDb")]
    [BsonIgnoreExtraElements(false)]
    public class AttributeStrictItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    /// <summary> Carries no attribute, so it shows what the registered convention does on its own. </summary>
    [EntityDatabase("ConventionTestDb")]
    public class AttributeFreeControlItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    /// <summary> Declares tolerance on the type while a strict convention is registered. </summary>
    [EntityDatabase("ConventionTestDb")]
    [BsonIgnoreExtraElements]
    public class AttributeTolerantItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }
}

namespace MongoRepository.Tests.ConventionScopes.Included
{
    [EntityDatabase("ConventionTestDb")]
    public class IncludedItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }
}

namespace MongoRepository.Tests.ConventionScopes.Excluded
{
    [EntityDatabase("ConventionTestDb")]
    public class ExcludedItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }
}

namespace MongoRepository.Tests.ConventionScopes.Orders
{
    [EntityDatabase("ConventionTestDb")]
    public class ScopedOrder : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }
}

namespace MongoRepository.Tests.ConventionScopes.Orders.Nested
{
    [EntityDatabase("ConventionTestDb")]
    public class NestedOrder : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }
}

namespace MongoRepository.Tests.ConventionScopes.OrdersArchive
{
    [EntityDatabase("ConventionTestDb")]
    public class ArchivedOrder : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }
}

namespace MongoRepository.Tests.ConventionScopes.Filtered
{
    [EntityDatabase("ConventionTestDb")]
    public class FilterAcceptedItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class FilterRejectedItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }
}

namespace MongoRepository.Tests.ConventionScopes.Combined
{
    [EntityDatabase("ConventionTestDb")]
    public class AcceptedInNamespaceItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class RejectedInNamespaceItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }
}

namespace MongoRepository.Tests.ConventionScopes.Outside
{
    [EntityDatabase("ConventionTestDb")]
    public class AcceptedOutsideNamespaceItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }
}

namespace MongoRepository.Tests.ConventionScopes.Fallback
{
    [EntityDatabase("ConventionTestDb")]
    public class FallbackFirstItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class FallbackFirstOfTwoItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class FallbackSecondOfTwoItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class FallbackNeutralItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class FallbackLateItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class FallbackAfterNoOpItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }
}

namespace MongoRepository.Tests.ConventionScopes.Lifecycle
{
    [EntityDatabase("ConventionTestDb")]
    public class BeforeUnregisterItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }

    [EntityDatabase("ConventionTestDb")]
    public class AfterUnregisterItem : IEntity<string>
    {
        [BsonId]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
    }
}
