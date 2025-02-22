﻿// Copyright (c) IEvangelist. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

namespace Microsoft.Azure.CosmosRepository.Specification.Evaluator
{
    internal interface IEvaluator
    {
        bool IsFilterEvaluator { get; }

        IQueryable<TItem> GetQuery<TItem, TResult>(
            IQueryable<TItem> query,
            ISpecification<TItem, TResult> specification)
            where TItem : IItem
            where TResult : IQueryResult<TItem>;
    }
}
