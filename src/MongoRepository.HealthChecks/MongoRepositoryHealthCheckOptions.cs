namespace MongoRepository.HealthChecks
{
    /// <summary>
    /// Options to control how the MongoRepository health check evaluates failures.
    /// </summary>
    public sealed class MongoRepositoryHealthCheckOptions
    {
        /// <summary>
        /// If true: a single failing connection (read-only OR read/write) results in Unhealthy.
        /// If false: a single failure results in Degraded (default).
        /// </summary>
        public bool SingleFailureIsUnhealthy { get; set; } = false;

        /// <summary>
        /// If true: a missing (empty) connection string counts as a failure.
        /// If false: a missing connection string is treated as 'skipped' and does not degrade health (default).
        /// </summary>
        public bool MissingConnectionIsFailure { get; set; } = false;
    }
}