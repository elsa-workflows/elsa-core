import 'reflect-metadata';
import {h} from "@stencil/core";
import {EventTypes, Plugin} from "../../models";
import {Container, Service} from "typedi";
import {EventBus, StudioService} from "../../services";


@Service()
export class HomePagePlugin implements Plugin {
  private studioService: StudioService;

  constructor(studioService: StudioService) {
    this.studioService = studioService;

    const eventBus = Container.get(EventBus);
    eventBus.on(EventTypes.Auth.SignedIn, this.onSignedIn);
  }

  async initialize(): Promise<void> {
  }

  private onSignedIn = () => {
    const studioService = Container.get(StudioService);
    studioService.show(() => <elsa-home-page/>);
  }

}
