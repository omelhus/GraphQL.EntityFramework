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
            return query;

        var selections = BuildFieldSelector(new[] { FieldContext.FromContext(context) }).ToHashSet();
        if (keys?.Any() == true)
            foreach (var key in keys)
                selections.Add(key!);

        var selector = ExpressionHelper.BuildSelector<TItem, TItem>(selections);
        return query.Select(selector);
    }

    static bool IsConnectionNode(FieldContext field)
    {
        var name = field.Name.ToLowerInvariant();
        return name is "edges" or "items" or "node";
    }

    public IEnumerable<string> BuildFieldSelector(IEnumerable<FieldContext> fields, FieldContext? parent = null)
    {
        foreach (var field in fields)
        {
            if (parent != null && !IsConnectionNode(field))
            {
                if (!field.FieldDefinition.Metadata.TryGetValue("_EF_PropertyName", out var selectName))
                    selectName = field.Name;
                if (parent != null && !IsConnectionNode(parent))
                {
                    if (!parent.FieldDefinition.Metadata.TryGetValue("_EF_PropertyName", out var parentName))
                        parentName = parent.Name;
                    yield return $"{parentName}.{selectName}";
                }
                else
                {
                    yield return $"{selectName}";
                }
            }
            foreach (var sub in BuildFieldSelector(field.Fields, field))
                yield return sub;
        }
    }
}
