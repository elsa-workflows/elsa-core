# Serialization and capacity limits

## Workflow JSON aliases

Workflow JSON accepts registered type aliases and supported compound and legacy CLR type names up to 4,096 characters and 64 type composition markers. Both current collection and array syntax and legacy CLR type names share this budget. A type alias outside the budget is rejected during deserialization. Format ordinary data as plain JSON or use shorter registered aliases for application types. Plain JSON values outside type metadata keep the serializer's existing JSON behavior.

Existing workflow definitions and persisted data using aliases within these limits require no changes. If archived data contains a larger alias, convert it to a shorter registered alias before importing it. Newly generated aliases use the same limits on the read and write paths.
