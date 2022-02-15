﻿using GraphQL;
using GraphQL.EntityFramework;
using Microsoft.EntityFrameworkCore.Metadata;

public class SelectAppender
{
    private IModel model;

    public SelectAppender(IModel model)
    {
        this.model = model;
    }

    public IQueryable<TItem> AddSelect<TItem>(IQueryable<TItem> query, IEnumerable<string>? keys, IResolveFieldContext context, bool isConnection = false)
       where TItem : class
    {
        if (context.SubFields is null)
        {
            return query;
        }

        var selections = context.GetQuerySelections();

        if (keys?.Any() == true)
            foreach (var key in keys)
                selections.Add(key!);

        var selector = ExpressionHelper.BuildSelector<TItem, TItem>(selections);
        query = query.Select(selector);

        return query;
    }

    internal static void SetIncludeMetadata(Type type, string selectName, IDictionary<string, object?> metadata)
    {
        if (!metadata.TryGetValue("_EF_SelectName", out var setObject))
        {
            metadata["_EF_SelectName"] = new HashSet<(Type type, string name)>(new[] { (type, selectName) });
        }
        else if (setObject is HashSet<(Type type, string name)> set)
        {
            set.Add((type, selectName));
        }
    }
}

public static class SelectAppenderExtensions
{
    public static void IncludeField<T>(this IResolveFieldContext<T> context, string fieldName)
    {
        SelectAppender.SetIncludeMetadata(typeof(T), fieldName, context.UserContext);
    }
}
