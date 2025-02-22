﻿// Copyright (c) IEvangelist. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.CosmosRepository.Extensions;
using Microsoft.Azure.CosmosRepository.Options;
using Microsoft.Azure.CosmosRepository.Providers;
using Microsoft.Azure.CosmosRepository.Validators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.CosmosRepository.Services
{
    class DefaultCosmosContainerService : ICosmosContainerService
    {
        readonly ICosmosItemConfigurationProvider _cosmosItemConfigurationProvider;
        readonly ICosmosClientProvider _cosmosClientProvider;
        readonly ILogger<DefaultCosmosContainerService> _logger;
        readonly RepositoryOptions _options;
        private readonly Dictionary<string, DateTime> _containerSyncLog = new();

        public DefaultCosmosContainerService(ICosmosItemConfigurationProvider cosmosItemConfigurationProvider,
            ICosmosClientProvider cosmosClientProvider,
            IOptions<RepositoryOptions> options,
            ILogger<DefaultCosmosContainerService> logger,
            IRepositoryOptionsValidator repositoryOptionsValidator)
        {
            repositoryOptionsValidator.ValidateForContainerCreation(options);
            _cosmosItemConfigurationProvider = cosmosItemConfigurationProvider;
            _cosmosClientProvider = cosmosClientProvider;
            _logger = logger;
            _options = options.Value;
        }

        public Task<Container> GetContainerAsync<TItem>(bool forceContainerSync = false) where TItem : IItem =>
            GetContainerAsync(typeof(TItem), forceContainerSync);

        private async Task<Container> GetContainerAsync(Type itemType, bool forceContainerSync = false)
        {
            try
            {
                ItemConfiguration itemConfiguration = _cosmosItemConfigurationProvider.GetItemConfiguration(itemType);

                Database database =
                    await _cosmosClientProvider.UseClientAsync(
                        client => client.CreateDatabaseIfNotExistsAsync(_options.DatabaseId)).ConfigureAwait(false);

                ContainerProperties containerProperties = new()
                {
                    Id = _options.ContainerPerItemType
                        ? itemConfiguration.ContainerName
                        : _options.ContainerId,
                    PartitionKeyPath = itemConfiguration.PartitionKeyPath,
                    UniqueKeyPolicy = itemConfiguration.UniqueKeyPolicy ?? new(),
                    DefaultTimeToLive = itemConfiguration.DefaultTimeToLive
                };

                Container container =
                    await database.CreateContainerIfNotExistsAsync(
                        containerProperties, itemConfiguration.ThroughputProperties).ConfigureAwait(false);

                if ((itemConfiguration.SyncContainerProperties is false || _containerSyncLog.ContainsKey(container.Id)) && forceContainerSync is false)
                {
                    return container;
                }

                await container.ReplaceThroughputAsync(itemConfiguration.ThroughputProperties);
                await container.ReplaceContainerAsync(containerProperties);

                if (itemConfiguration.SyncContainerProperties)
                {
                    _containerSyncLog.Add(container.Id, DateTime.UtcNow);
                }

                return container;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get container with error {GetContainerError}", ex.Message);
                throw;
            }
        }

        public Task<Container> GetContainerAsync(IReadOnlyList<Type> itemTypes)
        {
            itemTypes.AreAllItems();

            if (itemTypes.Any() is false)
            {
                throw new InvalidOperationException("You must provided at least one item type to get a container for");
            }

            string containerName = _cosmosItemConfigurationProvider.GetItemConfiguration(itemTypes.First()).ContainerName;

            if(itemTypes.Select(x => _cosmosItemConfigurationProvider.GetItemConfiguration(x)).All(x => x.ContainerName == containerName))
            {
                return GetContainerAsync(itemTypes.First());
            }

            throw new InvalidOperationException(
                "The item types provided are not all configured to use the same container");
        }
    }
}