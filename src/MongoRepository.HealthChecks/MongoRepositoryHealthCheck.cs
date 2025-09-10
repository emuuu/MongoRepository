using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MongoRepository.HealthChecks
{
    /// <summary>
    /// Health check that validates connectivity for both read-only and read/write Mongo connections.
    /// </summary>
    public class MongoRepositoryHealthCheck : IHealthCheck
    {
        private readonly MongoDbOptions _options;
        private readonly MongoRepositoryHealthCheckOptions _behavior;

        public MongoRepositoryHealthCheck(
            IOptions<MongoDbOptions> options,
            MongoRepositoryHealthCheckOptions behavior = null)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _behavior = behavior ?? new MongoRepositoryHealthCheckOptions();
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>();

            bool readOnlyOk = false;
            bool readWriteOk = false;
            Exception readOnlyEx = null;
            Exception readWriteEx = null;

            static string ResolveDatabaseName(string connectionString)
            {
                var url = MongoUrl.Create(connectionString);
                return string.IsNullOrWhiteSpace(url.DatabaseName) ? "admin" : url.DatabaseName;
            }

            // Read-only
            if (!string.IsNullOrWhiteSpace(_options.ReadOnlyConnection))
            {
                try
                {
                    var roClient = new MongoClient(_options.ReadOnlyConnection);
                    var db = roClient.GetDatabase(ResolveDatabaseName(_options.ReadOnlyConnection));
                    await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
                    readOnlyOk = true;
                }
                catch (Exception ex)
                {
                    readOnlyEx = ex;
                }
            }
            else
            {
                if (_behavior.MissingConnectionIsFailure)
                {
                    readOnlyEx = new InvalidOperationException("ReadOnlyConnection not configured.");
                }
                else
                {
                    data["readOnlySkipped"] = true;
                    readOnlyOk = true;
                }
            }

            // Read-write
            if (!string.IsNullOrWhiteSpace(_options.ReadWriteConnection))
            {
                try
                {
                    var rwClient = new MongoClient(_options.ReadWriteConnection);
                    var db = rwClient.GetDatabase(ResolveDatabaseName(_options.ReadWriteConnection));
                    await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
                    readWriteOk = true;
                }
                catch (Exception ex)
                {
                    readWriteEx = ex;
                }
            }
            else
            {
                if (_behavior.MissingConnectionIsFailure)
                {
                    readWriteEx = new InvalidOperationException("ReadWriteConnection not configured.");
                }
                else
                {
                    data["readWriteSkipped"] = true;
                    readWriteOk = true;
                }
            }

            data["readOnly"] = readOnlyOk ? "ok" : "failed";
            data["readWrite"] = readWriteOk ? "ok" : "failed";
            data["singleFailureIsUnhealthy"] = _behavior.SingleFailureIsUnhealthy;
            data["missingConnectionIsFailure"] = _behavior.MissingConnectionIsFailure;

            bool bothOk = readOnlyOk && readWriteOk;
            bool bothFailed = !readOnlyOk && !readWriteOk;

            if (bothOk)
                return HealthCheckResult.Healthy("MongoRepository connections healthy", data);

            if (bothFailed)
            {
                var aggregate = new AggregateException(readOnlyEx, readWriteEx);
                return HealthCheckResult.Unhealthy("All MongoRepository connections failed", aggregate, data);
            }

            // Exactly one failed
            var singleFailureException = readOnlyEx ?? readWriteEx;
            if (_behavior.SingleFailureIsUnhealthy)
                return HealthCheckResult.Unhealthy("One MongoRepository connection failed", singleFailureException, data);

            return HealthCheckResult.Degraded("One MongoRepository connection failed", singleFailureException, data);
        }
    }
}