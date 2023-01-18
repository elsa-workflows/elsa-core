import 'reflect-metadata';
import {h} from "@stencil/core";
import {Service as MiddlewareService} from 'axios-middleware';
import {EventTypes, Plugin} from "../../models";
import {Container, Service} from "typedi";
import {StudioService, AuthContext, EventBus, ElsaClient, ElsaApiClientProvider} from "../../services";
import descriptorsStore from '../../data/descriptors-store';
import {SignedInArgs} from "./models";
import {AxiosInstance} from "axios";
import {LoginApi} from "./services";

@Service()
export class LoginPlugin implements Plugin {
  private readonly eventBus: EventBus;
  private readonly studioService: StudioService;
  private elsaClient: ElsaClient;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.studioService = Container.get(StudioService);
    this.eventBus.on(EventTypes.HttpClient.ClientCreated, this.onHttpClientCreated);
  }

  async initialize(): Promise<void> {
    const authContext = Container.get(AuthContext);

    if (!authContext.getIsSignedIn()) {
      this.studioService.show(() => <elsa-login-page onSignedIn={this.onSignedIn}/>);
    } else {
      await this.loadDescriptors();
    }
  }

  private loadDescriptors = async (): Promise<void> => {
    const elsaClientProvider = Container.get(ElsaApiClientProvider);
    this.elsaClient = await elsaClientProvider.getElsaClient();

    const activityDescriptors = await this.elsaClient.descriptors.activities.list();
    const storageDrivers = await this.elsaClient.descriptors.storageDrivers.list();
    const variableDescriptors = await this.elsaClient.descriptors.variables.list();
    const workflowInstantiationStrategyDescriptors = await this.elsaClient.descriptors.workflowActivationStrategies.list();

    descriptorsStore.activityDescriptors = activityDescriptors;
    descriptorsStore.storageDrivers = storageDrivers;
    descriptorsStore.variableDescriptors = variableDescriptors;
    descriptorsStore.workflowActivationStrategyDescriptors = workflowInstantiationStrategyDescriptors;
  };

  private onSignedIn = async (e: CustomEvent<SignedInArgs>) => await this.loadDescriptors();

  private onHttpClientCreated = async (e) => {
    const service: MiddlewareService = e.service;
    const loginApi = Container.get(LoginApi);

    service.register({
      async onRequest(request) {
        const authContext = Container.get(AuthContext);
        const token = authContext.getAccessToken();

        if (!!token)
          request.headers = {...request.headers, Authorization: `Bearer ${token}`};

        return request;
      },

      async onResponseError(error) {
        if (error.response.status !== 401 || error.response.config.hasRetriedRequest) 
          return;

        const authContext = Container.get(AuthContext);
        const loginResponse = await loginApi.refreshAccessToken(authContext.getRefreshToken());
        if (loginResponse.isAuthenticated) {
          
          await authContext.signinTokens(loginResponse.accessToken, loginResponse.refreshToken, true);

          const t = await service.http({
            ...error.config,
            hasRetriedRequest: true,
            headers: {
              ...error.config.headers,
              Authorization: `Bearer ${loginResponse.accessToken}`
            }
          });

          return t;
        }
        else {
          authContext.signOut();
        }
      }
    });
  };

}
