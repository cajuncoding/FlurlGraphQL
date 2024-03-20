using System;

namespace FlurlGraphQL.Tests.Models
{
    internal enum Gender { Male, Female }

    internal class Author
    {
        public Guid AuthorGlobalId { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public int AuthoredBookCount { get; set; }
        public bool IsRetired { get; set; }
        public bool  IsNewYorTimesBestSeller { get; set; }
        public decimal AverageAnnualSales { get; set; }
        public string SummaryBiography { get; set; }
        public Book[] AuthoredBooks { get; set; }
        public Book[] EditedBooks { get; set; }

    }

    internal class Book
    {
        public Guid BookUUID { get; set; }
        public string Name { get; set; }
        public DateTime PublishedDate { get; set; }
        public int PageCount { get; set; }
        public string BookSynopsis { get; set; }
        public bool IsNewYorkTimesBestseller { get; set; }
        public Author[] Authors { get; set; }
        public Author[] Editors { get; set; }
    }
}
