﻿using GraphQL.Types;
using EfCoreGraphQL;

public class Query : ObjectGraphType
{
    public Query()
    {
        this.AddQueryField<CompanyGraph, Company>("companies",
            resolve: context =>
            {
                var dataContext = (MyDataContext) context.UserContext;
                return dataContext.Companies;
            });

        this.AddQueryField<EmployeeGraph, Employee>("employees",
            resolve: context =>
            {
                var dataContext = (MyDataContext) context.UserContext;
                return dataContext.Employees;
            });
    }
}