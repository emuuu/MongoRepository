using System;

namespace MongoRepository
{
    /// <summary>	Attribute for entity name. </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EntityDatabaseAttribute : Attribute
    {
        /// <summary>	Gets or sets the name. </summary>
        /// <value>	The name of the entity. </value>
        public string Database { get; set; }

        /// <summary>	Default constructor. </summary>
        public EntityDatabaseAttribute()
        {
        }

        /// <summary>	Constructor. </summary>
        /// <param name="database">	The database of the entity. </param>
        public EntityDatabaseAttribute(string database)
        {
            Database = database;
        }
    }
}
