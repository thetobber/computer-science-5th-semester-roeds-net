using MongoDB.Driver;
using Roeds.Models;

namespace Roeds.Interfaces {
    public interface IMongoContext {
        IMongoCollection<Property> Properties { get; }
    }
}
