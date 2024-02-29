using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace FlurlGraphQL.Tests.Models
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

    internal class StarWarsCharacterWithJsonMappings
    {
        [JsonPropertyName("personalIdentifier")] public int MyPersonalIdentifier { get; set; }
        [JsonPropertyName("name")] public string MyName { get; set; }
        [JsonPropertyName("height")] public decimal MyHeight { get; set; }
        [JsonPropertyName("friends")] public List<StarWarsCharacterWithJsonMappings> MyFriends { get; set; }
        [JsonPropertyName("cursor")] public string MyCursor { get; set; }
    }


    // ReSharper disable once ClassNeverInstantiated.Local
    internal class StarWarsCharacterWithNestedPagingResult
    {
        public int PersonalIdentifier { get; set; }
        public string Name { get; set; }
        public decimal Height { get; set; }
        public IGraphQLConnectionResults<StarWarsCharacter> Friends { get; set; }
    }
}
