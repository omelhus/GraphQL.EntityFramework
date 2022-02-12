using GraphQL;
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

        var selections = context.GetQuerySelections()
            .Where(x => x.Contains('.'))
            .Select(x => x.Substring(x.IndexOf('.') + 1))
            .Select(x =>
            {
                if (!isConnection) return x;
                if (x.StartsWith("items") && x.Length > 6)
                {
                    return x.Substring(6);
                }
                else if (x.StartsWith("edges") && x.Length > 11)
                {
                    return x.Substring(11);
                }
                return string.Empty;
            })
            .Where(x => x != string.Empty)
            .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

        if (keys?.Any() == true)
            foreach (var key in keys)
                selections.Add(key!);

        var selector = ExpressionHelper.BuildSelector<TItem, TItem>(selections);
        query = query.Select(selector);

        return query;
    }
}
