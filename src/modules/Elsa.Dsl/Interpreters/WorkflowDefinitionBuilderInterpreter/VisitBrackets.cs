﻿using System.Collections.Generic;
using System.Linq;
using Elsa.Dsl.Extensions;
using Elsa.Workflows.Core.Services;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    public override IWorkflowBuilder VisitBracketsExpr(ElsaParser.BracketsExprContext context)
    {
        var propertyType = _expressionType.Get(context.Parent);
        var targetElementType = propertyType.GetGenericArguments().First();
        var contents = context.exprList().expr();

        var items = contents.Select(x =>
        {
            Visit(x);
            var objectContext = x.GetChild<ElsaParser.ObjectContext>(0);
            return _expressionValue.Get(objectContext);
        }).ToList();

        var stronglyTypedListType = typeof(ICollection<>).MakeGenericType(targetElementType);
        var stronglyTypedList = items.ConvertTo(stronglyTypedListType);

        _expressionValue.Put(context, stronglyTypedList);
        return DefaultResult;
    }
}