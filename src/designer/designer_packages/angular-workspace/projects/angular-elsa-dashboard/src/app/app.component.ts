import { AfterViewInit, Component, ElementRef, OnInit, ViewChild, ViewContainerRef } from '@angular/core';
import { WorkflowDefinition,WorkflowDefinitionsPlugin,StudioService,Container,ActivityDescriptorManager,ServerSettings,ElsaClientProvider, LoginApi, AuthContext,JSX, Components, WorkflowDefinitionsApi, WorkflowDefinitionSummary } from '@elsa-workflows/elsa-workflows-designer';
import { ElsaStudio, ElsaWorkflowDefinitionEditor } from '@elsa-workflows/elsa-workflows-designer-angular-wrapper';


@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit{
    elsaContainer! : StudioService;
    workflowDefinitionsPlugin:WorkflowDefinitionsPlugin  = Container.get(WorkflowDefinitionsPlugin);
    worflowApi: WorkflowDefinitionsApi=Container.get(WorkflowDefinitionsApi);
    workflowList!: WorkflowDefinitionSummary[];
    wf!:WorkflowDefinitionSummary;

    async ngOnInit(): Promise<void> {
        let serverSettings = Container.get(ServerSettings);
        serverSettings.baseAddress = "https://localhost:7228/elsa/api";

        let loginApi = Container.get(LoginApi);
        let loginResponse = await loginApi.login("admin","password");

        const accessToken = loginResponse.accessToken;
        const refreshToken = loginResponse.refreshToken;
        const authContext = Container.get(AuthContext);
        await authContext.signin(accessToken, refreshToken, true);
        console.log(await authContext.getIsSignedIn());
        const provider = Container.get(ElsaClientProvider);
         const http = await provider.getHttpClient();

        //Add Interceptor
        http.interceptors.request.use(request=>{
            const authContext = Container.get(AuthContext);
            const token = authContext.getAccessToken();

            if (!!token)
              request.headers = {...request.headers, Authorization: `Bearer ${token}`} as any;

            return request;
        })

        //refresh availables activities
        let activityDescriptorManager = Container.get(ActivityDescriptorManager);
        activityDescriptorManager.refresh();

        this.workflowList =  await (await this.worflowApi.list({versionOptions:{isLatest:true}})).items;

  }

  async loadWorkflow(definitionId : string){
    let workflow = await this.worflowApi.get({
        definitionId : definitionId
    });

    this.workflowDefinitionsPlugin.showWorkflowDefinitionEditor(workflow);
  }

  async newWorkflow(){
    this.workflowDefinitionsPlugin.newWorkflow();
  }
  async deleteWorkflow(definitionId: string){
    await this.worflowApi.delete({definitionId:definitionId});
  }
  publish(){
    console.log("publish?");
    this.workflowDefinitionsPlugin.publishCurrentWorkflow({
        begin:()=>{console.log("begin")},
        complete : ()=> {console.log("complete")}
    })
  }

  async onWorkflowChange(event: WorkflowDefinitionSummary){
        console.log(event.definitionId);
        await this.loadWorkflow(event.definitionId);
  }


  title = 'AngularElsaDashboard';

}


