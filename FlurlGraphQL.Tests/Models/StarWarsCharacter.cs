using System.Collections.Generic;
using System.ComponentModel;
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

    internal enum EnumTestCase
    {
        //Use Description to Override Behavior
        [EnumMember(Value = "TEST_PASCAL_CASE_ENUM_MEMBER_OVERRIDE")]
        TestPacalCaseEnumMember = 0,
        //Use JsonPropertyName to Override Behavior
        [JsonPropertyName("TEST.JsonPropertyName.Supported.By.System.Text.Json")]
        TestJsonPropertyNameSupportedBySystemTextJson = 8,
        //Auto-Coversion tests with no attribute...
        TestPascalCaseAutoConversion = 2,
        //Auto-Coversion tests with no attribute...
        TestV2 = 3,
        //Auto-Coversion tests with no attribute...
        Test__Strange__Pascal__Snake__Case__2 = 4,
        //Auto-Coversion tests with no attribute...
        testCamelCase = 5,
        //Auto-Coversion tests with no attribute...
        test_snake_case = 6,
        //Auto-Coversion tests with no attribute...
        TEST_SCREAMING_SNAKE_CASE = 7,
        //JsonPropertyAttribute Test...
        [Description("TEST.Description.Attribute")]
        TestDescriptionAttribute = 9
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
