// Copyright (c) IEvangelist. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CosmosEventSourcing.Projections;
using Microsoft.Azure.CosmosEventSourcingAcceptanceTests.Events;
using Microsoft.Azure.CosmosEventSourcingAcceptanceTests.Items;
using Microsoft.Azure.CosmosRepository;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.CosmosEventSourcingAcceptanceTests.Projections;

public static class TodoItemProjections
{
    public class Created : IDomainEventProjectionBuilder<TodoListCreated, TodoListEventItem>
    {
        private readonly IWriteOnlyRepository<TodoListItem> _repository;
        private readonly ILogger<Created> _logger;

        public Created(
            IWriteOnlyRepository<TodoListItem> repository,
            ILogger<Created> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async ValueTask HandleAsync(
            TodoListCreated created,
            TodoListEventItem eventItem,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("TodoListCreated being processed with ID {TodoId} and name {TodoName}",
                eventItem.Id,
                created.Name);

            await _repository.CreateAsync(new TodoListItem(created.Name), cancellationToken);
        }
    }

    public class Added : IDomainEventProjectionBuilder<TodoItemAdded, TodoListEventItem>
    {
        private readonly IWriteOnlyRepository<TodoCosmosItem> _repository;
        private readonly ILogger<Added> _logger;

        public Added(
            IWriteOnlyRepository<TodoCosmosItem> repository,
            ILogger<Added> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async ValueTask HandleAsync(
            TodoItemAdded domainEvent,
            TodoListEventItem eventItem,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("TodoItemAdded being processed with ID {TodoId} and title {TodoTitle}",
                domainEvent.Id,
                domainEvent.Title);

            await _repository.UpdateAsync(new TodoCosmosItem(
                domainEvent.Id,
                domainEvent.Title,
                domainEvent.OccuredUtc,
                eventItem.PartitionKey), cancellationToken);
        }
    }

    public class Completed : IDomainEventProjectionBuilder<TodoItemCompleted, TodoListEventItem>
    {
        private readonly IRepository<TodoCosmosItem> _repository;
        private readonly ILogger<Completed> _logger;

        public Completed(
            IRepository<TodoCosmosItem> repository,
            ILogger<Completed> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async ValueTask HandleAsync(
            TodoItemCompleted domainEvent,
            TodoListEventItem eventItem,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("TodoItemCompleted being processed with ID {TodoId} and title {TodoTitle}",
                domainEvent.Id,
                domainEvent.Title);

            TodoCosmosItem item = await _repository.GetAsync(
                domainEvent.Id.ToString(),
                eventItem.PartitionKey,
                cancellationToken);

            item.CompletedAt = domainEvent.OccuredUtc;

            await _repository.UpdateAsync(item, cancellationToken);
        }
    }
}