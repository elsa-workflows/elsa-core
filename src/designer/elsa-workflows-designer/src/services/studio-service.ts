import 'reflect-metadata';
import {Service} from "typedi";
import studioComponentStore from "../data/studio-component-store";

@Service()
export class StudioService {
  show(componentFactory: () => any){
    studioComponentStore.activeComponentFactory = componentFactory
  }
}
