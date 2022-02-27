using GraphQL.Types;
using GraphQL.Types.Relay;

namespace GraphQL.EntityFramework;

/// <summary>
/// A connection graph type for the specified node graph type. The GraphQL type name
/// defaults to {NodeType}Connection where {NodeType} is the GraphQL type name of
/// the node graph type. This graph type assumes that the source (the result of
/// the parent field's resolver) is <see cref="ConnectionType{TNodeType, TEdgeType}"/>
/// or <see cref="ConnectionType{TNodeType}"/> or has the same members.
/// </summary>
/// <typeparam name="TNodeType">The graph type of the result data set's data type.</typeparam>
/// <typeparam name="TEdgeType">The edge graph type of node, typically <see cref="EdgeType{TNodeType}"/>.</typeparam>
public class EfGenericConnectionType<TNodeType, TEdgeType> : ConnectionType<TNodeType, TEdgeType>
        where TNodeType : IGraphType
        where TEdgeType : EfGenericEdgeType<TNodeType>
{
    /// <inheritdoc/>
    public EfGenericConnectionType()
    {
        var graphQLTypeName = typeof(TNodeType).GraphQLName();
        if (typeof(TNodeType).IsGenericType && typeof(TNodeType).GetGenericArguments()[0].IsGenericType)
        {
            var type = typeof(TNodeType).GetGenericArguments()[0];
            graphQLTypeName = type.GraphQLName();
            if (type.IsGenericType)
            {
                graphQLTypeName = type.GetGenericArguments()[0].GraphQLName();
            }
        }
        Name = $"{graphQLTypeName}Connection";
        Description =
            $"A connection from an object to a list of objects of type `{graphQLTypeName}`.";
    }
}

/// <summary>
/// A connection graph type for the specified node type. The GraphQL type name
/// defaults to {NodeType}Connection where {NodeType} is the GraphQL type name of
/// the node graph type. The edge graph type used is <see cref="EdgeType{TNodeType}"/>.
/// </summary>
/// <typeparam name="TNodeType">The graph type of the result data set's data type.</typeparam>
public class EfGenericConnectionType<TNodeType> : EfGenericConnectionType<TNodeType, EfGenericEdgeType<TNodeType>>
    where TNodeType : IGraphType
{

}
