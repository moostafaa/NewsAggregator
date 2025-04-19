using System.Collections.Generic;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Domain.Management.ValueObjects
{
    public class CloudCredentials : ValueObject
    {
        public string AccessKeyId { get; private set; }
        public string SecretAccessKey { get; private set; }
        public string TenantId { get; private set; } // For Azure
        public string ClientId { get; private set; } // For Azure
        public string ClientSecret { get; private set; } // For Azure

        private CloudCredentials(
            string accessKeyId,
            string secretAccessKey,
            string tenantId = null,
            string clientId = null,
            string clientSecret = null)
        {
            AccessKeyId = accessKeyId;
            SecretAccessKey = secretAccessKey;
            TenantId = tenantId;
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public static CloudCredentials CreateAWSCredentials(string accessKeyId, string secretAccessKey)
        {
            if (string.IsNullOrWhiteSpace(accessKeyId))
                throw new DomainException("AWS Access Key Id cannot be empty");

            if (string.IsNullOrWhiteSpace(secretAccessKey))
                throw new DomainException("AWS Secret Access Key cannot be empty");

            return new CloudCredentials(accessKeyId, secretAccessKey);
        }

        public static CloudCredentials CreateAzureCredentials(string tenantId, string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new DomainException("Azure Tenant Id cannot be empty");

            if (string.IsNullOrWhiteSpace(clientId))
                throw new DomainException("Azure Client Id cannot be empty");

            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new DomainException("Azure Client Secret cannot be empty");

            return new CloudCredentials(null, null, tenantId, clientId, clientSecret);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return AccessKeyId;
            yield return SecretAccessKey;
            yield return TenantId;
            yield return ClientId;
            yield return ClientSecret;
        }
    }
} 