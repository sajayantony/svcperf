namespace EtlViewer.QueryFx
{
    using System;
    using System.Collections.Generic;
    using EtlViewer.QueryFx;

    class SymbolHelper
    {
        static Dictionary<Guid, Resolver> Cache = new Dictionary<Guid, Resolver>();

        public static Resolver GetResolver(EventRecordProxy evt)
        {
            Resolver resolver;
            if (SymbolHelper.TryGetValue(evt.ProviderId, out resolver))
            {
                return resolver;
            }
            return SymbolHelper.Empty;
        }

        public static Resolver[] Parse(IEnumerable<string> manifests)
        {
            List<Resolver> resolvers = new List<Resolver>();
            foreach (var manifest in manifests)
            {
                ManifestParser manifestResolver = new ManifestParser();
                foreach (var resolver in manifestResolver.Parse(manifest))
                {
                    resolvers.Add(resolver);
                    Cache[resolver.ProviderId] = resolver;
                };
            }

            return resolvers.ToArray();
        }

        internal static bool TryGetValue(Guid guid, out Resolver resolver)
        {
            return Cache.TryGetValue(guid, out resolver);
        }

        public static Resolver Empty = new EmptyResolver();
    }
}
