# Dynamic Activity Providers

This sample project demonstrates how to implement a dynamic activity provider.

## Overview

The key to providing activities dynamically to the activity registry is to implement the `IActivityProvider` interface. This interface defines a single method, `GetDescriptorsAsync`, which returns a list of activity descriptors.

In this example, we are loading API endpoint definitions from a JSON file located in the `Data` folder and create an activity descriptor for each endpoint.
The activity descriptor is then used to create an activity type, which is then registered with the activity registry.

This example could be applied to any other data source, such as a database or a web service, Open API specifications, GraphQL, etc.

## Usage

1. Run the sample project.
2. Open the workflow editor.
3. Notice the new activities in the toolbox.
4. Drag one of the new activities onto the canvas.
5. Notice the new activity in the canvas.
6. Run the workflow.
7. Notice the new activity being executed.

## Notes

This project provides sample workflows that you can import into the workflow editor. The workflows are located in the `Workflows` folder.
You can then use the workflow editor to inspect the workflows and see how the activities are used.
When imported, you can invoke the workflows using the sample-request.http file located in the root folder containing this project.