import 'reflect-metadata';
import {h} from "@stencil/core";
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
    const axios = e.httpClient;
    const loginApi = Container.get(LoginApi);

    axios.interceptors.request.use(async config => {
      const authContext = Container.get(AuthContext);
      const token = authContext.getAccessToken();

      if (!!token)
        config.headers = {...config.headers, Authorization: `Bearer ${token}`};

      return config;
    });

    axios.interceptors.response.use(async response => {
      return response;
    }, async error => {

      if (error.response.status !== 401 || error.response.config.hasRetriedRequest)
        return;

      const authContext = Container.get(AuthContext);
      const loginResponse = await loginApi.refreshAccessToken(authContext.getRefreshToken());

      if (loginResponse.isAuthenticated) {
        await authContext.updateTokens(loginResponse.accessToken, loginResponse.refreshToken, true);

        return await axios.request({
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
    });
  };

}
