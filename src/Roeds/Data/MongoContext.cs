using Roeds.Interfaces;
using Roeds.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace Roeds.Data {
    public class MongoContext : IMongoContext {
        private readonly IMongoDatabase _database;

        public MongoContext(IOptions<Settings> settings) {
            var client = new MongoClient(settings.Value.ConenctionString);

            if (client != null) {
                _database = client.GetDatabase(settings.Value.Database);
            }
        }

        // Get Properties collection from context
        public IMongoCollection<Property> Properties {
            get {
                return _database.GetCollection<Property>("Properties");
            }
        }
    }
}