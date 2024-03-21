using System;

namespace FlurlGraphQL.Tests.Models
{
    internal class GraphQLBooksAndAuthors
    {
        public Guid BookUUID { get; set; }
        public string Name { get; set; }
        public DateTime PublishedDate { get; set; }
        public int PageCount { get; set; }
        public string BookSynopsis { get; set; }
        public bool IsNewYorkTimesBestseller { get; set; }
        public GraphQLConnectionTestResults<GraphQLAuthor> Authors { get; set; }
        public GraphQLConnectionTestResults<GraphQLAuthor> Editors { get; set; }
    }

    internal class GraphQLAuthor
    {
        public Guid AuthorGlobalId { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public int AuthoredBookCount { get; set; }
        public bool IsRetired { get; set; }
        public bool IsNewYorTimesBestSeller { get; set; }
        public decimal AverageAnnualSales { get; set; }
        public string SummaryBiography { get; set; }
        public GraphQLConnectionTestResults<GraphQLBooksAndAuthors> AuthoredBooks { get; set; }
        public GraphQLConnectionTestResults<GraphQLBooksAndAuthors> EditedBooks { get; set; }

    }
}
