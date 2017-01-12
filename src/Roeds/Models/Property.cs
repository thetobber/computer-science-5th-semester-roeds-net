using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Roeds.Models {
    public class Property {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        [BindNever]
        public string Id { get; set; }

        [BindNever]
        public DateTime Created { get; set; }

        [BindNever]
        public DateTime Modified { get; set; }

        [BindNever]
        public bool Validated { get; set; }

        [Required]
        public string CaseNumber { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string Type { get; set; }

        public IDictionary<string, object> Values { get; set; }
    }
}