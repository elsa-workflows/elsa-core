import 'reflect-metadata';
import {h} from "@stencil/core";
import {Service as MiddlewareService} from 'axios-middleware';
import {EventTypes, Plugin} from "../../models";
import {Container, Service} from "typedi";
import {StudioService, AuthContext, EventBus, ElsaClient, ElsaApiClientProvider} from "../../services";
import descriptorsStore from '../../data/descriptors-store';

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
      this.studioService.show(() => <elsa-login-page/>);
    }
    else {
      const elsaClientProvider = Container.get(ElsaApiClientProvider);
      this.elsaClient = await elsaClientProvider.getElsaClient();

      const activityDescriptors = await this.elsaClient.descriptors.activities.list();
      const storageDrivers = await this.elsaClient.descriptors.storageDrivers.list();

      descriptorsStore.activityDescriptors = activityDescriptors;
      descriptorsStore.storageDrivers = storageDrivers;
    }
  }

  private onHttpClientCreated = async (e) => {
    const service: MiddlewareService = e.service;
    const studioService = this.studioService;

    service.register({
      async onRequest(request) {
        const authContext = Container.get(AuthContext);
        const token = authContext.getAccessToken();

        if (!!token)
          request.headers = {...request.headers, 'Authorization': `Bearer ${token}`};

        return request;
      },

      async onResponseError(error) {
        debugger;
        if (error.response.status !== 401)
          return;

        studioService.show(() => <elsa-login-page/>);
      }
    });
  };

}
