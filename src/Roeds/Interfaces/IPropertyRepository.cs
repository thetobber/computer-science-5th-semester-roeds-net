using Roeds.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roeds.Interfaces
{
    public interface IPropertyRepository
    {
        Task<Property> Get(string id);
        Task<IEnumerable<Property>> GetAll(int? page, int? range, string s);
        void Create(Property property);
        Task<bool> Update(string id, Property property);
        Task<bool> Delete(string id);
    }
}
