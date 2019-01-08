## Elsa Workflows - Alpha

Elsa Workflows is a set of lean & mean workflowing components that you can use in your own .NET Core applications. It's heavily inspired by Orchard Core Workflows, Azure Logic Apps and Windows Workflow Foundation, but with some key differentiators.

**Preview Material:** This project not yet ready for actual use, but you are welcome to have a quick peek.
You can try out the web based designer today by following these steps:

1. Clone the repository.
2. Run NPM install on all folders containing packages.json (or run `node npm-install.js` - a script in the root that recursively installs the Node packages)
3. Open a shell and navigate to `src/samples/SampleDashboard.Web` and run `dotnet run`.
4. Navigate to https://localhost:44397/

When you click on the Workflows menu item in the dashboard, you'll see a Create Workflow button you can click to create your first workflow.

TODO: 
- Describe all the features (core engine, runtime, webbased designer, YAML, scripting, separation of designer from invoker from engine).
- Describe various use cases.
- Describe how to use.
- Describe architecture.
- Describe how to implement (custom host, custom dashboard).
- Implement more activities (fork, join, script, HTTP request, loops, etc.)
- Implement integration with Orchard Core (separate repo)
- Detailed documentation

![Web-based workflow designer](/doc/workflow-sample-1.png)
