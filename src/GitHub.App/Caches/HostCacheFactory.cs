﻿using System;
using System.ComponentModel.Composition;
using System.IO;
using Rothko;

namespace GitHub
{
    [Export(typeof(IHostCacheFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class HostCacheFactory : IHostCacheFactory
    {
        readonly Lazy<IBlobCacheFactory> blobCacheFactory;
        readonly Lazy<IOperatingSystemFacade> operatingSystem;
        
        [ImportingConstructor]
        public HostCacheFactory(Lazy<IBlobCacheFactory> blobCacheFactory, Lazy<IOperatingSystemFacade> operatingSystem)
        {
            this.blobCacheFactory = blobCacheFactory;
            this.operatingSystem = operatingSystem;
        }

        public IHostCache Create(HostAddress hostAddress)
        {
            var environment = OperatingSystem.Environment;
            // For GitHub.com, the cache file name should be "api.github.com.cache.db"
            // This is why we use ApiUrl and not CredentialCacheHostKey
            string host = hostAddress.ApiUri.Host;
            string cacheFileName = host + ".cache.db";

            var localMachinePath = Path.Combine(environment.GetLocalGitHubApplicationDataPath(), cacheFileName);
            var userAccountPath = Path.Combine(environment.GetApplicationDataPath(), cacheFileName);

            // CreateDirectory is a noop if the directory already exists.
            new[] { localMachinePath, userAccountPath }
                .ForEach(x => OperatingSystem.Directory.CreateDirectory(Path.GetDirectoryName(x)));

            var localMachineCache = BlobCacheFactory.CreateBlobCache(localMachinePath);
            var userAccountCache = BlobCacheFactory.CreateBlobCache(userAccountPath);

            return new HostCache(localMachineCache, userAccountCache);
        }

        IOperatingSystemFacade OperatingSystem { get { return operatingSystem.Value; } }
        IBlobCacheFactory BlobCacheFactory { get { return blobCacheFactory.Value; } }
    }
}
