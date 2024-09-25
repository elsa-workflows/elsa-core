# Server

This project demonstrates how to use Workflow Contexts.

A Workflow Context represents a custom, application-specific object provided to the workflow at runtime.
For example, if your workflow handles a Customer, a custom workflow context provider could provide this customer automatically to the workflow without the need for custom activities that load & persist updates to this customer.
Instead, the custom context provider would load the customer into memory once before the workflow starts and persists changes made to the customer (if any) when the workflow execution ends.

In this sample project, we handle two custom objects: Customer and Order.

To start the `CustomerCommunicationsWorkflow`, start it using the REST API or from Elsa Studio.

## Secrets
The following are the secrets stored in hashed form in appsettings.json:

**API key**: `4E753976726458745954355043687772-e54d5a2c-33a3-4c05-a216-b09569062aed`
**Admin user**: `admin`
**Admin password**: `password`