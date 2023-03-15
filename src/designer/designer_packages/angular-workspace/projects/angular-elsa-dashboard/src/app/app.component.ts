import { Component } from '@angular/core';
import { WorkflowDefinition } from '@elsa-workflows/elsa-workflows-designer';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'AngularElsaDashboard';

  workflowId : any = {
    "id": "3971e7fcc9f94c189cc142e3715b7d1d",
    "definitionId": "b7bb1a27fe40439aa6edc885cfaa1f6a",
    "name": "Workflow 1 test",
    "createdAt": "2023-03-13T16:21:10.4801749+00:00",
    "version": 4,
    "variables": [
        {
            "name": "Variable1",
            "typeName": "Object",
            "isArray": false,
            "value": "",
            "storageDriverTypeName": "Elsa.Workflows.Core.Services.WorkflowStorageDriver, Elsa.Workflows.Core"
        }
    ],
    "inputs": [],
    "outputs": [],
    "outcomes": [],
    "metadata": {},
    "isLatest": true,
    "isPublished": false,
    "root": {
        "type": "Elsa.Flowchart",
        "version": 1,
        "id": "Flowchart1",
        "metadata": {},
        "applicationProperties": {},
        "start": "FlowJoin1",
        "activities": [
            {
                "mode": {
                    "typeName": "Elsa.Workflows.Core.Activities.Flowchart.Models.FlowJoinMode, Elsa.Workflows.Core",
                    "expression": {
                        "type": "Literal",
                        "value": "WaitAll"
                    },
                    "memoryReference": {
                        "id": "FlowJoin1:input-1"
                    }
                },
                "id": "FlowJoin1",
                "type": "Elsa.FlowJoin",
                "version": 1,
                "canStartWorkflow": false,
                "runAsynchronously": false,
                "customProperties": {},
                "metadata": {
                    "designer": {
                        "position": {
                            "x": 1400,
                            "y": 2480
                        }
                    }
                }
            },
            {
                "cases": [],
                "id": "Switch1",
                "type": "Elsa.Switch",
                "version": 1,
                "canStartWorkflow": false,
                "runAsynchronously": false,
                "customProperties": {},
                "metadata": {
                    "designer": {
                        "position": {
                            "x": 2400,
                            "y": 2460
                        }
                    }
                }
            },
            {
                "cases": [],
                "id": "FlowSwitch1",
                "type": "Elsa.FlowSwitch",
                "version": 1,
                "canStartWorkflow": false,
                "runAsynchronously": false,
                "customProperties": {},
                "metadata": {
                    "designer": {
                        "position": {
                            "x": 1740,
                            "y": 2480
                        }
                    }
                }
            },
            {
                "text": {
                    "typeName": "String",
                    "expression": {
                        "type": "Literal",
                        "value": "azdazjdnazojdnazojn - from designer"
                    },
                    "memoryReference": {
                        "id": "WriteLine1:input-1"
                    }
                },
                "id": "WriteLine1",
                "type": "Elsa.WriteLine",
                "version": 1,
                "canStartWorkflow": false,
                "runAsynchronously": false,
                "customProperties": {},
                "metadata": {
                    "designer": {
                        "position": {
                            "x": 1960,
                            "y": 2580
                        }
                    }
                }
            }
        ],
        "connections": [
            {
                "source": "WriteLine1",
                "target": "Switch1",
                "sourcePort": "Done",
                "targetPort": "In"
            },
            {
                "source": "FlowSwitch1",
                "target": "WriteLine1",
                "sourcePort": "Done",
                "targetPort": "In"
            }
        ]
    }
}
}
