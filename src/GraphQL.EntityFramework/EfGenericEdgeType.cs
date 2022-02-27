using GraphQL.Types;
using GraphQL.Types.Relay;

namespace GraphQL.EntityFramework;

public class EfGenericEdgeType<TNodeType> : EdgeType<TNodeType>
        where TNodeType : IGraphType
{
    /// <inheritdoc/>
    public EfGenericEdgeType()
    {
        string graphQLTypeName = typeof(TNodeType).GraphQLName();
        if (typeof(TNodeType).IsGenericType && typeof(TNodeType).GetGenericArguments()[0].IsGenericType)
        {
            var type = typeof(TNodeType).GetGenericArguments()[0];
            graphQLTypeName = type.GraphQLName();
            if (type.IsGenericType)
            {
                graphQLTypeName = type.GetGenericArguments()[0].GraphQLName();
            }
        }
        Name = $"{graphQLTypeName}Edge";
        Description =
            $"An edge in a connection from an object to another object of type `{graphQLTypeName}`.";
    }
}
