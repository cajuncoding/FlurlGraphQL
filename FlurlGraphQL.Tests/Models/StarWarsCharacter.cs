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

    internal class StarWarsCharacterWithEnumMember
    {
        public int PersonalIdentifier { get; set; }
        public string Name { get; set; }
        public decimal Height { get; set; }
        public List<StarWarsCharacterWithEnumMember> Friends { get; set; }
        public string Cursor { get; set; }
        public TheForceWithEnumMembers TheForce { get; set; }
    }

    internal enum TheForceWithEnumMembers
    {
        [EnumMember(Value = "NONE")]
        NoneAtAll = 0,
        [EnumMember(Value = "LIGHT_SIDE")]
        LightSideForGood = 1,
        [EnumMember(Value = "DARK_SIDE")]
        DarkSideForEvil = -1
    }

    internal class StarWarsCharacterWithSimpleEnum
    {
        public int PersonalIdentifier { get; set; }
        public string Name { get; set; }
        public decimal Height { get; set; }
        public List<StarWarsCharacterWithSimpleEnum> Friends { get; set; }
        public string Cursor { get; set; }
        public TheForce TheForce { get; set; }
    }

    internal enum TheForce
    {
        None = 0,
        LightSide = 1,
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
