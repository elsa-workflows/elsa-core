# Workflow import limits

Studio accepts workflow JSON and ZIP files. ZIP uploads default to 10 MiB per file, including direct calls to the ZIP importer. A ZIP file and all of its nested archives share a 50 MiB expansion budget. The budget counts each JSON entry and each nested ZIP as their bytes are read. ZIP imports also accept up to 1,024 entries and eight archive levels, including the outermost archive.

Hosts calling the importer can adjust `ImportOptions.MaxAllowedSize` and `ImportOptions.MaxArchiveExpandedSize` in bytes. If an import reaches a limit, it stops processing that archive and returns an error. Workflow definitions imported earlier in that archive remain imported. Split larger archives into separate imports to use the default limits.
