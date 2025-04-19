using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using FlurlGraphQL.Tests.Models;

namespace FlurlGraphQL.Benchmarks.TestData
{
    internal class BooksAndAuthorsTestDataGenerator
    {
        public GraphQLConnectionTestResponse<GraphQLBooksAndAuthors> GenerateConnectionPagedMockData()
        {
            var books = CreateBookFaker(1).Generate(100);
            var connectionResults = ToGraphQLConnectionResults(books, new Faker(), 432822146);
            return new GraphQLConnectionTestResponse<GraphQLBooksAndAuthors>("getBooks", connectionResults);
        }

        public string GenerateJsonSource()
        {
            var data = GenerateConnectionPagedMockData();
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = false,
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            });

            return json;
        }

        public void GenerateAndWriteToTestDataJsonFile()
        {
            var json = GenerateJsonSource();
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), @"TestData\BooksAndAuthorsCursorPaginatedLargeDataSet.json"), json);
        }

        internal Faker<GraphQLBooksAndAuthors> CreateBookFaker(int callLevel, int maxLevels = 4)
        {
            var bookFaker = new Faker<GraphQLBooksAndAuthors>()
                .RuleFor(b => b.BookUUID, f => f.Random.Guid())
                .RuleFor(b => b.IsNewYorkTimesBestseller, f => f.Random.Bool(weight: .15f))
                .RuleFor(b => b.Name, f => f.Lorem.Text())
                .RuleFor(b => b.PublishedDate, f => f.Date.Past())
                .RuleFor(b => b.BookSynopsis, f => f.Lorem.Paragraphs(1, f.Random.Int(1, 3)))
                .RuleFor(b => b.PageCount, f => f.Random.Int(35, 1500));

            if(callLevel < maxLevels) bookFaker
                .RuleFor(b => b.Authors, f =>
                {
                    var authors = CreateAuthorFaker(callLevel + 1, maxLevels).Generate(f.Random.Int(0, 5));
                    return ToGraphQLConnectionResults(authors, f, maxTotalCount: 5);
                })
                .RuleFor(b => b.Editors, fake =>
                {
                    var authors = CreateAuthorFaker(callLevel + 1, maxLevels).Generate(fake.Random.Int(0, 3));
                    return ToGraphQLConnectionResults(authors, fake, maxTotalCount: 5);
                });

            return bookFaker;
        }

        internal Faker<GraphQLAuthor> CreateAuthorFaker(int callLevel, int maxLevels = 4)
        {
            var authorFaker = new Faker<GraphQLAuthor>()
                .RuleFor(a => a.AuthorGlobalId, f => f.Random.Guid())
                .RuleFor(a => a.FirstName, f => f.Name.FirstName())
                .RuleFor(a => a.MiddleName, f => f.PickRandom(f.Name.FirstName(), f.Name.LastName()))
                .RuleFor(a => a.LastName, f => f.Name.LastName())
                .RuleFor(a => a.Gender, f => f.PickRandom<Gender>())
                .RuleFor(a => a.BirthDate, f => f.Date.Past(100))
                .RuleFor(a => a.AuthoredBookCount, f => f.Random.Int(0, 500))
                .RuleFor(a => a.IsRetired, f => f.Random.Bool(weight: .35f))
                .RuleFor(a => a.IsNewYorTimesBestSeller, f => f.Random.Bool(weight: .15f))
                .RuleFor(a => a.AverageAnnualSales, fake => fake.Random.Decimal(0, 100M))
                .RuleFor(a => a.SummaryBiography, fake => fake.Lorem.Paragraphs(fake.Random.Int(1, 5)));

            if (callLevel < maxLevels) authorFaker
                .RuleFor(a => a.AuthoredBooks, f =>
                {
                    var books = CreateBookFaker(callLevel + 1, maxLevels).Generate(f.Random.Int(0, 10));
                    return ToGraphQLConnectionResults(books, f, maxTotalCount: 300);
                })
                .RuleFor(a => a.EditedBooks, f =>
                {
                    var books = CreateBookFaker(callLevel + 1, maxLevels).Generate(f.Random.Int(0, 5));
                    return ToGraphQLConnectionResults(books, f, maxTotalCount: 25);
                });

            return authorFaker;
        }

        internal static GraphQLConnectionTestResults<TEntity> ToGraphQLConnectionResults<TEntity>(IReadOnlyList<TEntity> entities, Faker fake, int maxTotalCount = 50) where TEntity : class
        {
            var totalCount = fake.Random.Int(entities.Count + 1, maxTotalCount);
            var pageInfo = new GraphQLCursorPageInfo(
                Guid.NewGuid().ToString("N"), //No Hyphens
                Guid.NewGuid().ToString("N"), //No Hyphens
                totalCount > entities.Count,
                fake.Random.Bool()
            );

            return new GraphQLConnectionTestResults<TEntity>(entities, pageInfo);
        }
    }
}
