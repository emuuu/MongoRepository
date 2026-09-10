namespace MongoRepository
{
    /// <summary>	Connection options for a mongoDB context. </summary>
    public class MongoDbOptions
    {
        /// <summary>   Gets or sets the connection string used for read and write operations. </summary>
        /// <value> The read/write connection string. </value>
        public string ReadWriteConnection { get; set; }

        /// <summary>   Gets or sets the connection string used for read-only operations. </summary>
        /// <value> The read-only connection string. </value>
        public string ReadOnlyConnection { get; set; }

        /// <summary>
        /// Gets or sets the serialization conventions the library registers with the driver.
        /// Initialised to an instance whose switches are all off, so leaving it untouched keeps
        /// the driver's own behaviour. Setting it to <c>null</c> turns the automatic fallback in
        /// <see cref="EntityContext{TEntity}"/> into a no-op and makes
        /// <see cref="MongoRepositoryConventions.Register(MongoDbOptions)"/> throw.
        /// </summary>
        /// <value> The serialization options. </value>
        public MongoSerializationOptions Serialization { get; set; } = new MongoSerializationOptions();
    }
}
