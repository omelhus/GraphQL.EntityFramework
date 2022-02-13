using GraphQL.Language.AST;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace GraphQL.EntityFramework;

public class ExpressionHelper
{
    public static Expression<Func<TSource, TTarget>> BuildSelector<TSource, TTarget>(string members) =>
        BuildSelector<TSource, TTarget>(members.Split(',').Select(m => m.Trim()));

    public static Expression<Func<TSource, TTarget>> BuildSelector<TSource, TTarget>(IEnumerable<string> members)
    {
        var parameter = Expression.Parameter(typeof(TSource), "e");
        var body = NewObject(typeof(TTarget), parameter, members.Select(m => m.Split('.')));
        return Expression.Lambda<Func<TSource, TTarget>>(body, parameter);
    }

    static ConcurrentDictionary<(Type type, string memberName), (string? name, MemberExpression? memberExpression)> MemberCache = new();

    static Expression NewObject(Type targetType, Expression source, IEnumerable<string[]> memberPaths, int depth = 0)
    {
        var bindings = new List<MemberBinding>();
        var target = Expression.Constant(null, targetType);

        foreach (var memberGroup in memberPaths.GroupBy(path => path[depth]))
        {

            var (memberName, targetMember) = MemberCache.GetOrAdd((targetType, memberGroup.Key), (key) =>
            {
                var property = targetType.GetProperty(key.memberName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    return (property.Name, Expression.PropertyOrField(target, property.Name));
                }
                return (null, null);
            });

            if (memberName == null || targetMember == null)
                continue;

            var sourceMember = Expression.PropertyOrField(source, memberName);
            var childMembers = memberGroup.Where(path => depth + 1 < path.Length).ToList();

            Expression targetValue = null!;
            if (!childMembers.Any())
                targetValue = sourceMember;
            else
            {
                if (IsEnumerableType(targetMember.Type, out var sourceElementType) &&
                    IsEnumerableType(targetMember.Type, out var targetElementType))
                {
                    var sourceElementParam = Expression.Parameter(sourceElementType, "e");
                    targetValue = NewObject(targetElementType, sourceElementParam, childMembers, depth + 1);
                    targetValue = Expression.Call(typeof(Enumerable), nameof(Enumerable.Select),
                        new[] { sourceElementType, targetElementType }, sourceMember,
                        Expression.Lambda(targetValue, sourceElementParam));

                    targetValue = CorrectEnumerableResult(targetValue, targetElementType, targetMember.Type);
                }
                else
                {
                    targetValue = NewObject(targetMember.Type, sourceMember, childMembers, depth + 1);
                }
            }

            if (sourceMember.Type.GetTypeInfo().IsClass)
            {
                var valueToCheck = Expression.PropertyOrField(source, memberName);
                var nullCheck = Expression.Equal(valueToCheck, Expression.Constant(null, sourceMember.Type));
                var checkForNull = Expression.Condition(nullCheck, Expression.Default(targetMember.Type), targetValue);
                bindings.Add(Expression.Bind(targetMember.Member, checkForNull));
            }
            else
            {
                bindings.Add(Expression.Bind(targetMember.Member, targetValue));
            }
        }
        return Expression.MemberInit(Expression.New(targetType), bindings);
    }

    static bool IsEnumerableType(Type type, out Type elementType)
    {
        foreach (var intf in type.GetInterfaces())
        {
            if (intf.IsGenericType && intf.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                elementType = intf.GetGenericArguments()[0];
                return true;
            }
        }

        elementType = null!;
        return false;
    }

    static bool IsSameCollectionType(Type type, Type genericType, Type elementType)
    {
        var result = genericType.MakeGenericType(elementType).IsAssignableFrom(type);
        return result;
    }

    static Expression CorrectEnumerableResult(Expression enumerable, Type elementType, Type memberType)
    {
        if (memberType == enumerable.Type)
            return enumerable;

        if (memberType.IsArray)
            return Expression.Call(typeof(Enumerable), nameof(Enumerable.ToArray), new[] { elementType }, enumerable);

        if (IsSameCollectionType(memberType, typeof(List<>), elementType)
            || IsSameCollectionType(memberType, typeof(ICollection<>), elementType)
            || IsSameCollectionType(memberType, typeof(IReadOnlyList<>), elementType)
            || IsSameCollectionType(memberType, typeof(IReadOnlyCollection<>), elementType))
            return Expression.Call(typeof(Enumerable), nameof(Enumerable.ToList), new[] { elementType }, enumerable);

        throw new NotImplementedException($"Not implemented transformation for type '{memberType.Name}'");
    }
    /// <summary>
    /// Generates a 't => new T { Member = t.Member, Member2 = t.Member2 }' type of expression, mostly used for EFCore Select queries.
    /// </summary>
    /// <param name="selectProperties"></param>
    /// <returns></returns>
    public static Expression<Func<T, T>> BuildSelectExpression<T>([NotNull] IEnumerable<string> selectProperties)
    {
        if (selectProperties == null)
            throw new ArgumentNullException(nameof(selectProperties));

        // t =>
        var x = Expression.Parameter(typeof(T), "x");

        var bindings = new List<MemberBinding>();

        //var properties = typeof(T).GetMembers().ToDictionary(l => l.Name, StringComparer.OrdinalIgnoreCase);
        // Member = t.Member
        // Member2 = a.Member2
        foreach (var sf in selectProperties)
        {
            var prop = PropertyCache<T>.GetProperty(sf);
            if (prop != null)
                bindings.Add(Expression.Bind(prop.Info, Expression.MakeMemberAccess(x, prop.Info)));
        }

        // => new TType { ... bindings ... }
        var newBody = Expression.MemberInit(Expression.New(typeof(T)), bindings);


        // t => new TType { Member = t.Member }
        return Expression.Lambda<Func<T, T>>(newBody, x);
    }
}

public static class ResolveFieldContextExtensions
{
    public static HashSet<string> GetQuerySelections(this IResolveFieldContext context)
    {
        var results = new HashSet<string>();

        foreach (var node in context.Operation.SelectionSet.Children)
        {
            if (node is Field f)
            {
                var subResults = ResolveFieldContextExtensions.GetQuerySelections(string.Empty, f);
                foreach (var subResult in subResults)
                {
                    results.Add(subResult);
                }
            }
        }
        return results;
    }

    private static HashSet<string> GetQuerySelections(string hierarchy, Field field)
    {
        var results = new HashSet<string>();

        hierarchy = string.Format("{0}{1}", hierarchy, field.Name);
        results.Add(hierarchy);

        foreach (var node in field.SelectionSet!.Children)
        {
            if (node is Field f)
            {
                var subResults = ResolveFieldContextExtensions.GetQuerySelections(hierarchy + ".", f);
                foreach (var subResult in subResults)
                {
                    results.Add(subResult);
                }
            }
        }

        return results;
    }
}
