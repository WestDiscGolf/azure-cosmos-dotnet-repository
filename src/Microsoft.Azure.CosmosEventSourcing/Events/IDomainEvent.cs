// Copyright (c) IEvangelist. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.CosmosEventSourcing.Events;

/// <summary>
/// An event that occurs inside a domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    string EventName { get; }

    /// <summary>
    /// The sequence number in which the event occured.
    /// </summary>
    int Sequence { get; }

    /// <summary>
    /// The <see cref="DateTime"/> that this event occured
    /// </summary>
    DateTime OccuredUtc { get; }
}