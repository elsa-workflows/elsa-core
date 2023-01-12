## Installation
Refer to project `Elsa.Secrets.Persistence.MongoDb` and `Elsa.Secrets.Persistence.EntityFramework.Core`
Startup files and use the code there as example.

## UI
Inside index.html:
```
import { CredentialManagerPlugin } from "/build/index.esm.js";

...

const elsaStudioRoot = document.querySelector('elsa-studio-root');

elsaStudioRoot.addEventListener('initializing', e => {
  const elsa = e.detail;
  elsa.pluginManager.registerPlugins([
    ...
    CredentialManagerPlugin,
    ...
  ]);
 ...
```
`CredentialManagerPlugin` creates the `Credential manager` menu item into the top menu,
which leads you to the page, where you can create and edit credentials/secrets.


## Modules
1. Elsa.Secrets.Http
2. Elsa.Secrets.Sql

### Http module
Enable feature `Secrets:Http`

Affected activities:
1. SendHttpRequest

Turns `Authorization` field into dropdown and gives it options.


### Sql module
Enable feature `Secrets:Sql`

Affected activities:
1. ExecuteSqlCommand
2. ExecuteSqlQuery

Turns `Connection string` field into dropdown and gives it options.

### Notes
The secrets get matched to the affected activities by the `Type` + `Name` fields.
Make sure you do not create secrets with the same name+type so as to avoid confusion.