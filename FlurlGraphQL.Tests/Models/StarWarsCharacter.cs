using System.Collections.Generic;
using System.Runtime.Serialization;
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

    internal class StarWarsCharacterWithEnum
    {
        public int PersonalIdentifier { get; set; }
        public string Name { get; set; }
        public decimal Height { get; set; }
        public List<StarWarsCharacterWithEnum> Friends { get; set; }
        public string Cursor { get; set; }
        public TheForce TheForce { get; set; }
    }

    internal enum TheForce
    {
        [EnumMember(Value = "NONE")]
        None = 0,
        [EnumMember(Value = "LIGHT_SIDE")]
        LightSide = 1,
        [EnumMember(Value = "DARK_SIDE")]
        DarkSide = -1
    }

    internal class StarWarsCharacterWithSystemTextJsonMappings
    {
        [JsonPropertyName("personalIdentifier")] public int MyPersonalIdentifier { get; set; }
        [JsonPropertyName("name")] public string MyName { get; set; }
        [JsonPropertyName("height")] public decimal MyHeight { get; set; }
        [JsonPropertyName("friends")] public List<StarWarsCharacterWithSystemTextJsonMappings> MyFriends { get; set; }
        [JsonPropertyName("cursor")] public string MyCursor { get; set; }
    }

    internal class StarWarsCharacterWithNewtonsoftJsonMappings
    {
        [JsonProperty("personalIdentifier")] public int MyPersonalIdentifier { get; set; }
        [JsonProperty("name")] public string MyName { get; set; }
        [JsonProperty("height")] public decimal MyHeight { get; set; }
        [JsonProperty("friends")] public List<StarWarsCharacterWithNewtonsoftJsonMappings> MyFriends { get; set; }
        [JsonProperty("cursor")] public string MyCursor { get; set; }
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
