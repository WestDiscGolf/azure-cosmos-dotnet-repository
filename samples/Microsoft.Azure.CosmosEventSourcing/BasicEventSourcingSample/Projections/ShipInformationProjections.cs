// Copyright (c) IEvangelist. All rights reserved.
// Licensed under the MIT License.

using BasicEventSourcingSample.Core;
using BasicEventSourcingSample.Infrastructure;
using BasicEventSourcingSample.Projections.Models;
using Microsoft.Azure.CosmosEventSourcing.Projections;
using Microsoft.Azure.CosmosRepository;

namespace BasicEventSourcingSample.Projections;

public class ShipInformationProjections
{
    public class ShipCreatedBuilder : IDomainEventProjectionBuilder<ShipEvents.ShipCreated, ShipEventItem>
    {
        private readonly IRepository<ShipInformation> _repository;

        public ShipCreatedBuilder(IRepository<ShipInformation> repository) =>
            _repository = repository;

        public async ValueTask HandleAsync(
            ShipEvents.ShipCreated domainEvent,
            ShipEventItem eventItem,
            CancellationToken cancellationToken = default)
        {
            ShipInformation info = new(
                domainEvent.Name,
                domainEvent.Commissioned,
                domainEvent.OccuredUtc);

            await _repository.UpdateAsync(info, cancellationToken);
        }
    }

    public class ShipDockedBuilder : IDomainEventProjectionBuilder<ShipEvents.DockedInPort, ShipEventItem>
    {
        private readonly IRepository<ShipInformation> _repository;

        public ShipDockedBuilder(IRepository<ShipInformation> repository) =>
            _repository = repository;

        public async ValueTask HandleAsync(
            ShipEvents.DockedInPort domainEvent,
            ShipEventItem eventItem,
            CancellationToken cancellationToken = default)
        {
            (string name, string port) = domainEvent;

            ShipInformation shipInfo = await _repository.GetAsync(
                name,
                nameof(ShipInformation),
                cancellationToken);

            shipInfo.LatestPort = port;

            await _repository.UpdateAsync(shipInfo, cancellationToken);
        }
    }

    public class ShipLoadedBuilder : IDomainEventProjectionBuilder<ShipEvents.Loaded, ShipEventItem>
    {
        private readonly IRepository<ShipInformation> _repository;

        public ShipLoadedBuilder(IRepository<ShipInformation> repository) =>
            _repository = repository;

        public async ValueTask HandleAsync(
            ShipEvents.Loaded domainEvent,
            ShipEventItem eventItem,
            CancellationToken cancellationToken = default)
        {
            (string name, string port, double cargoWeight) = domainEvent;

            ShipInformation shipInfo = await _repository.GetAsync(
                name,
                nameof(ShipInformation),
                cancellationToken);

            shipInfo.LatestPort = port;
            shipInfo.LatestCargoWeight = cargoWeight;

            await _repository.UpdateAsync(shipInfo, cancellationToken);
        }
    }
}