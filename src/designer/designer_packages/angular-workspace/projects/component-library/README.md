# elsa-workflows-designer-angular-wrapper

This library was generated with [Angular CLI](https://github.com/angular/angular-cli) version 15.1.0.

This package is a wrapper of the `@elsa-workflows/elsa-workflows-designer` v3. It allows you to embbed the designer in an Angular app and keep benefit of all the Angular features. 

## Usage

### Import Package

use `npm install @elsa-workflows/elsa-workflows-designer-angular-wrapper` to install the package.

you will also need `@elsa-workflows/elsa-workflows-designer` as a depencency. 

### import module

first in your app.module.ts add the following line : 

```typescript
import { ComponentLibraryModule } from '@elsa-workflows/elsa-workflows-designer-angular-wrapper';
```

and import the module in your `NgModule` :

```typescript
@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    FormsModule,
    ComponentLibraryModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
```

### Use the component

If you want to display only the provider, you only need the elsa studio component with 2 properties :
- `server`: uri of the api
- `monaco-lib-path` : link to your monaco package (here we use the cdnjs url)
 
```html
<elsa-studio server="https://localhost:7228/elsa/api"
      monaco-lib-path="https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.31.1/min">
</elsa-studio>
```

due to some coupled part between components, for now (elsa designer 3.0.2-preview.64), we need to use some Dependency Injection and configure some other properties to be sure that all designer services works correctly.

So in your `App Init` or `Component Init`, you should declare some code : 


Import the necessary services from the designer library

```typescript
import { Container } from '@elsa-workflows/elsa-workflows-designer';

```

Configure the server settings : 

```typescript
        let serverSettings = Container.get(ServerSettings);
        serverSettings.baseAddress = "https://localhost:7228/elsa/api";
```

If you don't want to use the Login Page, you've to log yourself (or if your use your specific jwt token or security) : 

To log in : 
```typescript
        let loginApi = Container.get(LoginApi);
        let loginResponse = await loginApi.login("admin","password");   
```

to get the token response :
```typescript
        const accessToken = loginResponse.accessToken;
        const refreshToken = loginResponse.refreshToken;
```

to signin (this kept information in browser session and emit signin event for other components) :
```typescript
await authContext.signin(accessToken, refreshToken, true);
```

to be sure that every http call is using your authentication token : 
```typescript
 const provider = Container.get(ElsaClientProvider);
         const http = await provider.getHttpClient();

        //Add Interceptor
        http.interceptors.request.use(request=>{
            const authContext = Container.get(AuthContext);
            const token = authContext.getAccessToken();
    
            if (!!token)
              request.headers = {...request.headers, Authorization: `Bearer ${token}`};
    
            return request;
        })
```

Then you can load the available activities : 
```typescript
        //refresh availables activities
        let activityDescriptorManager = Container.get(ActivityDescriptorManager);
        activityDescriptorManager.refresh();

```

and finally you can get the available workflow and load a workflow : 
```typescript
///get list 
let workflowList =  await (await this.worflowApi.list({versionOptions:{isLatest:true}})).items;
...

//get workflow definition using his Id : 
let workflowApi = Container.get(WorkflowDefinitionsApi);
let workflow = await this.worflowApi.get({
    definitionId : definitionId
});
    
//load workflow in the studio
let workflowDefinitionsPlugin = Container.get(WorkflowDefinitionsPlugin);
workflowDefinitionsPlugin.showWorkflowDefinitionEditor(workflow);
``` 