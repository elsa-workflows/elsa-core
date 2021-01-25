﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowDefinitions
{
    public class ManyWorkflowDefinitionVersionIdsSpecification : Specification<WorkflowDefinition>
    {
        public IEnumerable<string> Ids { get; set; }
        public ManyWorkflowDefinitionVersionIdsSpecification(IEnumerable<string> ids) => Ids = ids;
        public override Expression<Func<WorkflowDefinition, bool>> ToExpression() => x => Ids.Contains(x.DefinitionVersionId);
    }
}