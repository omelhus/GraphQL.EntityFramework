using GraphQL.Types;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GraphQL.EntityFramework;

public static class SelectAppenderExtensions
{
    public static FieldType WithRequiredFields(this FieldType field, params FieldType[] fields)
    {
        field.Metadata["_EF_Required_Fields"] = fields.Select(x => x.GetPropertyName()).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
        return field;
    }

    public static IEnumerable<string> GetRequiredFields(this FieldType field)
    {
        if (field.Metadata.TryGetValue("_EF_Required_Fields", out var requiredFieldsObject) && requiredFieldsObject is HashSet<string> fields)
            return fields;
        return Enumerable.Empty<string>();
    }

    public static string GetPropertyName(this FieldType field)
    {
        if (field.Metadata.TryGetValue("_EF_PropertyName", out var propertyName) && propertyName != null)
        {
            return $"{propertyName}";
        }
        return field.Name;
    }
}

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

    public IEnumerable<string> BuildFieldSelector(IEnumerable<FieldContext> fields, string? prefix = null, FieldContext? parent = null)
    {
        foreach (var field in fields)
        {
            var fieldPrefix = prefix;
            if (parent != null && !IsConnectionNode(field))
            {
                var selectName = field.FieldDefinition.GetPropertyName();
                if (prefix != null)
                {
                    yield return $"{prefix}.{selectName}";
                }
                else
                {
                    yield return $"{selectName}";
                }
                foreach (var requiredField in field.FieldDefinition.GetRequiredFields())
                {
                    yield return prefix != null ? $"{prefix}.{requiredField}" : requiredField;
                }
                fieldPrefix = prefix != null ? $"{prefix}.{selectName}" : selectName;
            }
            if (field.Fields.Any())
                foreach (var sub in BuildFieldSelector(field.Fields, fieldPrefix, field))
                    yield return sub;
        }
    }
}
