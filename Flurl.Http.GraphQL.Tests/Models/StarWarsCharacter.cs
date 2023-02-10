using System;
using System.Collections.Generic;

namespace Flurl.Http.GraphQL.Tests.Models
{
    // ReSharper disable once ClassNeverInstantiated.Local
    internal class StarWarsCharacter
    {
        public int PersonalIdentifier { get; set; }
        public string Name { get; set; }
        public decimal Height { get; set; }
        public List<StarWarsCharacter> Friends { get; set; }
        public string Cursor { get; set; }
    }
}
