import {ActivityModel, ActivityPropertyDescriptor, ElsaStudio} from "../models/";
import {PropertyDisplayDriver} from "./property-display-driver";
import {NullPropertyDriver } from "../drivers";
import {Map} from '../utils/utils';

export type PropertyDisplayDriverMap = Map<(elsaStudio: ElsaStudio) => PropertyDisplayDriver>;

export class PropertyDisplayManager {

  elsaStudio: ElsaStudio;
  initialized: boolean;
  drivers: PropertyDisplayDriverMap = {};

  initialize(elsaStudio: ElsaStudio) {
    if (this.initialized)
      return;

    this.elsaStudio = elsaStudio;
    this.initialized = true;
  }

  addDriver<T extends PropertyDisplayDriver>(controlType: string, driverFactory: (elsaStudio: ElsaStudio) => T) {
    this.drivers[controlType] = driverFactory;
  }

  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const driver = this.getDriver(property.uiHint);
    return driver.display(activity, property);
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
    const driver = this.getDriver(property.uiHint);
    const update = driver.update;
    
    if(!update)
      return;
    
    return update(activity, property, form);
  }

  getDriver(type: string) {
    const driverFactory = this.drivers[type] || ((_: ElsaStudio) => new NullPropertyDriver());
    return driverFactory(this.elsaStudio);
  }
}

export const propertyDisplayManager = new PropertyDisplayManager();
