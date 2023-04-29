import 'reflect-metadata';
import {h} from "@stencil/core";
import {Service as MiddlewareService} from 'axios-middleware';
import {EventTypes, Plugin} from "../../models";
import {Container, Service} from "typedi";
import {StudioService, AuthContext, EventBus} from "../../services";
import {LoginApi} from "./services";

@Service()
export class LoginPlugin implements Plugin {
  private readonly eventBus: EventBus;
  private readonly studioService: StudioService;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.studioService = Container.get(StudioService);
    this.eventBus.on(EventTypes.HttpClient.ClientCreated, this.onHttpClientCreated);
  }

  async initialize(): Promise<void> {
    const authContext = Container.get(AuthContext);

    if (authContext.getIsSignedIn())
      return;

    this.studioService.show(() => <elsa-login-page/>);
  }

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

          await authContext.updateTokens(loginResponse.accessToken, loginResponse.refreshToken, true);

          return await service.http({
            ...error.config,
            hasRetriedRequest: true,
            headers: {
              ...error.config.headers,
              Authorization: `Bearer ${loginResponse.accessToken}`
            }
          });
        } else {
          await authContext.signOut();
        }
      }
    });
  };

}
