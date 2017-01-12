using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Roeds.Models;
using Microsoft.Extensions.Caching.Memory;
using Roeds.Interfaces;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace Roeds.Controllers {
    [Route("[controller]")]
    public class PropertiesController : Controller {
        private readonly IPropertyRepository _repository;
        private readonly IMemoryCache _cache;
        private string _cacheKey;

        // Injecting repository and cache
        public PropertiesController(IPropertyRepository repository, IMemoryCache cache) {
            _repository = repository;
            _cache = cache;
        }

        //[Authorize]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery]int? page, [FromQuery]int? range, [FromQuery]string s, [FromQuery]int? caching) {
            if (caching != null && caching == 0) {
                var noCacheResult = await _repository.GetAll(page, range, s);

                if (noCacheResult == null)
                    return NotFound();

                return Ok(noCacheResult);
            }

            SetCacheKey();
            IEnumerable<Property> result;

            if (!_cache.TryGetValue(_cacheKey, out result)) {
                result = await _repository.GetAll(page, range, s);

                if (result == null)
                    return NotFound();

                _cache.Set(
                    _cacheKey,
                    result,
                    new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(1))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                );
            }

            return Ok(result);
        }

        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> Get(string id, [FromQuery]int? caching) {
            if (caching != null && caching == 0) {
                var noCacheResult = await _repository.Get(id);

                if (noCacheResult == null)
                    return NotFound();

                return Ok(noCacheResult);
            }

            SetCacheKey();
            Property result;

            if (!_cache.TryGetValue(_cacheKey, out result)) {
                result = await _repository.Get(id);

                if (result == null)
                    return NotFound();

                _cache.Set(
                    _cacheKey,
                    result,
                    new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(1))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                );
            }

            return Ok(result);
        }

        [HttpPost]
        public IActionResult Post([FromBody]Property property) {
            if (!ModelState.IsValid)
                return BadRequest();

            _repository.Create(new Property() {
                Created = DateTime.Now,
                Modified = DateTime.Now,
                Validated = false,
                CaseNumber = property.CaseNumber,
                Type = property.Type,
                Address = property.Address,
                Values = property.Values
            });

            // 201 Created
            return StatusCode(201);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Put(string id, [FromBody]Property property) {
            var result = await _repository.Update(id, property);

            if (!result)
                return BadRequest();

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id) {
            var result = await _repository.Delete(id);

            if (!result)
                return BadRequest();

            return NoContent();
        }

        private void SetCacheKey() {
            _cacheKey = UriHelper.GetEncodedUrl(Request);
        }
    }
}