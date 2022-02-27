using GraphQL.Caching;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using ExecutionContext = GraphQL.Execution.ExecutionContext;

namespace GraphQL.EntityFramework;

public class EfDocumentExecuter :
    DocumentExecuter
{
    public EfDocumentExecuter(IDocumentBuilder documentBuilder,
        IDocumentValidator documentValidator,
        ComplexityAnalyzer complexityAnalyzer,
        IDocumentCache documentCache) : base(documentBuilder, documentValidator, complexityAnalyzer, documentCache)
    {

    }
    public EfDocumentExecuter()
    {

    }

    protected override IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
    {
        if (context.Operation.OperationType == OperationType.Query)
        {
            return new SerialExecutionStrategy();
        }

        return base.SelectExecutionStrategy(context);
    }
}