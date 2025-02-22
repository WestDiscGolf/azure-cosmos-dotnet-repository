// Copyright (c) IEvangelist. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using BasicEventSourcingSample.Core;
using BasicEventSourcingSample.Projections.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.CosmosEventSourcing;
using Microsoft.Azure.CosmosEventSourcing.Stores;
using Microsoft.Azure.CosmosRepository;

namespace BasicEventSourcingSample.Infrastructure;

public class ShipRepository : IShipRepository
{
    private readonly IEventStore<ShipEventItem> _store;
    private readonly IRepository<ShipInformation> _shipInformationRepository;

    public ShipRepository(
        IEventStore<ShipEventItem> store,
        IRepository<ShipInformation> shipInformationRepository)
    {
        _store = store;
        _shipInformationRepository = shipInformationRepository;
    }

    public async ValueTask<Ship> FindAsync(string shipName)
    {
        IEnumerable<ShipEventItem> sourcedEvents = await _store.ReadAsync(shipName);

        Ship ship = Ship.Build(sourcedEvents.Select(x =>
            x.DomainEvent).ToList());

        return ship;
    }

    public ValueTask SaveAsync(Ship ship)
    {
        List<ShipEventItem> events = ship
            .NewEvents
            .Select(x =>
                new ShipEventItem(x, ship.Name))
            .ToList();

        events.Add(new ShipEventItem(ship.AtomicEvent, ship.Name));

        return _store.PersistAsync(events);
    }

    public async Task<IEnumerable<string>> GetShipNamesAsync()
    {
        IEnumerable<ShipInformation> all = await _shipInformationRepository
            .GetAsync(x => x.PartitionKey == nameof(ShipInformation));

        return all.Select(x => x.Name);
    }

    public async ValueTask<ShipInformation?> GetInformationAsync(string name) =>
        await _shipInformationRepository.TryGetAsync(name, nameof(ShipInformation));
}