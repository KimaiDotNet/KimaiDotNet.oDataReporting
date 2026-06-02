using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Extensions
{
    public sealed class DefaultErrorMappingRequestAdapter : IRequestAdapter
    {
        private static readonly Dictionary<string, ParsableFactory<IParsable>> DefaultErrorMappings = new()
        {
            { "4XX", UntypedNode.CreateFromDiscriminatorValue },
            { "5XX", UntypedNode.CreateFromDiscriminatorValue },
        };

        private readonly IRequestAdapter _inner;

        public DefaultErrorMappingRequestAdapter(IRequestAdapter inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public ISerializationWriterFactory SerializationWriterFactory => _inner.SerializationWriterFactory;

        public string? BaseUrl
        {
            get => _inner.BaseUrl;
            set => _inner.BaseUrl = value;
        }

        public void EnableBackingStore(IBackingStoreFactory backingStoreFactory)
        {
            _inner.EnableBackingStore(backingStoreFactory);
        }

        public Task<T?> SendAsync<T>(RequestInformation requestInfo,
            ParsableFactory<T> factory,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = default,
            CancellationToken cancellationToken = default)
            where T : IParsable
        {
            return _inner.SendAsync(requestInfo, factory, MergeErrorMapping(errorMapping), cancellationToken);
        }

        public Task<IEnumerable<T>?> SendCollectionAsync<T>(RequestInformation requestInfo,
            ParsableFactory<T> factory,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = default,
            CancellationToken cancellationToken = default)
            where T : IParsable
        {
            return _inner.SendCollectionAsync(requestInfo, factory, MergeErrorMapping(errorMapping), cancellationToken);
        }

        public Task<T?> SendPrimitiveAsync<T>(RequestInformation requestInfo,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = default,
            CancellationToken cancellationToken = default)
        {
            return _inner.SendPrimitiveAsync<T>(requestInfo, MergeErrorMapping(errorMapping), cancellationToken);
        }

        public Task<IEnumerable<T>?> SendPrimitiveCollectionAsync<T>(RequestInformation requestInfo,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = default,
            CancellationToken cancellationToken = default)
        {
            return _inner.SendPrimitiveCollectionAsync<T>(requestInfo, MergeErrorMapping(errorMapping), cancellationToken);
        }

        public Task SendNoContentAsync(RequestInformation requestInfo,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = default,
            CancellationToken cancellationToken = default)
        {
            return _inner.SendNoContentAsync(requestInfo, MergeErrorMapping(errorMapping), cancellationToken);
        }

        public Task<TNativeRequest?> ConvertToNativeRequestAsync<TNativeRequest>(RequestInformation requestInfo,
            CancellationToken cancellationToken = default)
        {
            return _inner.ConvertToNativeRequestAsync<TNativeRequest>(requestInfo, cancellationToken);
        }

        private static Dictionary<string, ParsableFactory<IParsable>> MergeErrorMapping(
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping)
        {
            if (errorMapping == null || errorMapping.Count == 0)
            {
                return DefaultErrorMappings;
            }

            bool has4xx = errorMapping.ContainsKey("4XX");
            bool has5xx = errorMapping.ContainsKey("5XX");
            if (has4xx && has5xx)
            {
                return errorMapping;
            }

            var merged = new Dictionary<string, ParsableFactory<IParsable>>(errorMapping);
            if (!has4xx)
            {
                merged["4XX"] = UntypedNode.CreateFromDiscriminatorValue;
            }

            if (!has5xx)
            {
                merged["5XX"] = UntypedNode.CreateFromDiscriminatorValue;
            }

            return merged;
        }
    }
}
