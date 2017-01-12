using Roeds.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Roeds.Models {
    public class PropertyRepository : IPropertyRepository {
        private readonly IMongoContext _context;

        // Inject settings into the contructor
        public PropertyRepository(IMongoContext context) {
            _context = context;
        }

        // Return a single document by id from the collection
        public async Task<Property> Get(string id) {
            var filter = Builders<Property>.Filter.Eq(c => c.Id, id);
            return await _context.Properties.Find(filter).FirstOrDefaultAsync();
        }

        // Return all documents in the collection
        public async Task<IEnumerable<Property>> GetAll(int? page, int? range, string s) {
            var builder = Builders<Property>.Filter;
            var _page = page < 0 ? 0 : page;
            var _range = range <= 0 ? 10 : range;

            if (string.IsNullOrEmpty(s))
                return await _context.Properties.Find(_ => true)
                    .Skip(_page * _range)
                    .Limit(_range)
                    .ToListAsync();

            var filter = Builders<Property>.Filter
                .Where(p => p.Address.ToLower()
                .Contains(s));

            return await _context.Properties.Find(filter)
                .Skip(_page * _range)
                .Limit(_range)
                .ToListAsync();
        }

        // Insert a document item into the collection
        public async void Create(Property property) {
            await _context.Properties.InsertOneAsync(property);
        }

        // Replace a single document by id from the collection
        public async Task<bool> Update(string id, Property property) {
            var result = await _context.Properties.UpdateOneAsync(
                Builders<Property>.Filter.Eq(c => c.Id, id),
                Builders<Property>.Update
                    .Set(c => c.Validated, false)
                    .Set(c => c.CaseNumber, property.CaseNumber)
                    .Set(c => c.Type, property.Type)
                    .Set(c => c.Address, property.Address)
                    .Set(c => c.Values, property.Values)
                    .CurrentDate(c => c.Modified)
            );

            return result.IsAcknowledged;
        }

        // Delete a single document by id from the collection
        public async Task<bool> Delete(string id) {
            var result = await _context.Properties.DeleteOneAsync(
                Builders<Property>.Filter.Eq(c => c.Id, id)
            );

            return result.DeletedCount > 0;
        }
    }
}