﻿using GraphQL.EntityFramework;

class NullFilters :
    Filters
{
    public static NullFilters Instance = new();

    internal override Task<IEnumerable<TEntity>> ApplyFilter<TEntity>(IEnumerable<TEntity> result, object userContext)
    {
        return Task.FromResult(result);
    }

    internal override Task<bool> ShouldInclude<TEntity>(object userContext, TEntity? item)
        where TEntity : class
    {
        return Task.FromResult(true);
    }
}