import 'reflect-metadata';
import {h} from "@stencil/core";
import {EventTypes, Plugin} from "../../models";
import {Container, Service} from "typedi";
import {AuthContext, EventBus, StudioService} from "../../services";

@Service()
export class HomePagePlugin implements Plugin {

  constructor(private studioService: StudioService, private authContext: AuthContext) {
    const eventBus = Container.get(EventBus);
    eventBus.on(EventTypes.Auth.SignedIn, this.onSignedIn);
  }

  async initialize(): Promise<void> {
    if (this.authContext.getIsSignedIn())
      this.showHomePage();
  }

  private showHomePage = () => {
    this.studioService.show(() => <elsa-home-page/>);
  }

  private onSignedIn = () => this.showHomePage()
}
