# Extensions source tip c2618de7 (MongoDB tenant isolation)

After the import preserved Extensions `807cd893`, Extensions `main` merged two MongoDB tenant-isolation fixes,
elsa-extensions#247 and #249, reaching `c2618de7b92b22ae7b23d872b58a20866219f69b`. #8526 carried them into the import:

- `c405a11b` applies the exact nine-file delta `807cd893..c2618de7`:
  - five stores under `src/extensions/persistence/Elsa.Persistence.MongoDb`;
  - four new test files under `test/extensions/modules/persistence/Elsa.MongoDb.UnitTests`.
  - Every mapped file is byte-identical to upstream.
- `f8ad6661` joins the upstream tip as a second parent without changing the tree.
- `3ae6568e` is the GitHub merge into the import branch. It keeps both parents.

The [eighth receipt](source-tip-refresh-2026-09-28-r8.json) pins those commits and each file's old and new source and
mapped blobs. `verify_import_source_tip_refresh_r8.py` fails if:
- the upstream delta reaches beyond these MongoDB paths;
- a mapped file stops matching upstream;
- the history join is lost;
- the active publisher workflows changed;
- `Elsa.sln` stops selecting the MongoDB tests.

The mapped Slack package proof requires it. `Elsa.MongoDb.UnitTests` passed 41/41 on net10.0 at the delta. No package was
published.
