using System.Collections.Generic;
using NewsAggregator.Domain.Common;

namespace NewsAggregator.Domain.News.ValueObjects
{
    public class NewsContent : ValueObject
    {
        public string Title { get; private set; }
        public string Body { get; private set; }
        public string Summary { get; private set; }

        private NewsContent(string title, string body, string summary)
        {
            Title = title;
            Body = body;
            Summary = summary;
        }

        public static NewsContent Create(string title, string body, string summary)
        {
            // Add validation rules here
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Title cannot be empty");

            if (string.IsNullOrWhiteSpace(body))
                throw new DomainException("Body cannot be empty");

            return new NewsContent(title, body, summary ?? string.Empty);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Title;
            yield return Body;
            yield return Summary;
        }
    }
} 