using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MongoRepository.HealthChecks
{
    /// <summary>
    /// Extension methods to register the MongoRepository health check.
    /// </summary>
    public static class MongoRepositoryHealthCheckBuilderExtensions
    {
        public static IHealthChecksBuilder AddMongoRepository(
            this IHealthChecksBuilder builder,
            string name = "mongo_repository",
            HealthStatus? failureStatus = null,
            IEnumerable<string> tags = null,
            TimeSpan? timeout = null)
        {
            return builder.AddMongoRepository(_ => { }, name, failureStatus, tags, timeout);
        }

        /// <summary>
        /// Adds the MongoRepository health check with behavior customization.
        /// </summary>
        public static IHealthChecksBuilder AddMongoRepository(
            this IHealthChecksBuilder builder,
            Action<MongoRepositoryHealthCheckOptions> configure,
            string name = "mongo_repository",
            HealthStatus? failureStatus = null,
            IEnumerable<string> tags = null,
            TimeSpan? timeout = null)
        {
            if (configure == null) configure = _ => { };

            var localOptions = new MongoRepositoryHealthCheckOptions();
            configure(localOptions);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var mongoOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbOptions>>();
                    return new MongoRepositoryHealthCheck(mongoOptions, localOptions);
                },
                failureStatus,
                tags,
                timeout));
        }
    }
}