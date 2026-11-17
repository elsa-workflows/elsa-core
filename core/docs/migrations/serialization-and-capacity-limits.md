# Serialization and capacity limits

## Workflow JSON aliases

Workflow JSON accepts registered type aliases and supported compound and legacy CLR type names up to 4,096 characters and 64 type composition markers. Both current collection and array syntax and legacy CLR type names share this budget. A type alias outside the budget is rejected during deserialization. Format ordinary data as plain JSON or use shorter registered aliases for application types. Plain JSON values outside type metadata keep the serializer's existing JSON behavior.

Existing workflow definitions and persisted data using aliases within these limits require no changes. If archived data contains a larger alias, convert it to a shorter registered alias before importing it. Newly generated aliases use the same limits on the read and write paths.

## EF Core bookmark queue options

An EF Core bookmark queue item with unreadable options is moved to the dead-letter store with reason `UnreadableOptions`. The original options remain in the dead-letter table for inspection, and the item cannot be replayed. The worker advances to other items in the same page. If the move fails, the original item stays in the active queue for a later attempt.

Valid plain JSON and supported legacy typed options remain readable and replayable. No schema migration is needed. This recovery applies to EF Core's serialized options. Dapper's JSON queue and MongoDB's BSON queue have separate storage paths.

## Bulk workflow definition dispatch

`POST /workflow-definitions/{definitionId}/bulk-dispatch` accepts between 1 and 100 instances per request. Omitting `count` dispatches one instance. A request with a count outside this range returns HTTP 400 before any instances are dispatched. A successful response lists the instance IDs accepted for dispatch.

Dispatches already accepted remain queued if the request is cancelled or a later dispatch fails. Split larger requests into batches and account for previously accepted work before retrying a partially completed request.

## Outbound HTTP response bodies

The HTTP Request, Flow HTTP Request, and Download File activities and the HTTP file cache downloader buffer at most 64 MiB per outbound response by default. The limit applies when the response declares a length and when the length is unknown. A response outside the limit follows the activity's existing HTTP request failure path. The HTTP client continues to apply its configured timeout across the request and response body.

Set `HttpActivityOptions.MaxResponseBodySize` to a positive byte count for deployments that need a different limit. A lower limit already configured on the HTTP client takes precedence. This setting does not apply to inbound HTTP request bodies.
