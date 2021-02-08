import {ActivityModel, ActivityPropertyDescriptor} from "../models/domain";
import {PropertyDisplayDriver} from "./property-display-driver";
import {TextPropertyDriver} from "../property-display-drivers/text-property-driver";
import {Map} from '../utils/utils';

export type PropertyDisplayDriverMap = Map<() => PropertyDisplayDriver>;

export class PropertyDisplayManager {

  drivers: PropertyDisplayDriverMap = {};

  constructor() {
    this.drivers = {
      'Text': () => new TextPropertyDriver()
    };
  }

  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const driver = this.getDriver(property.type);
    return driver.display(activity, property);
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
    const driver = this.getDriver(property.type);
    return driver.update(activity, property, form);
  }

  getDriver(type: string) {
    const driverFactory = this.drivers[type];
    return driverFactory();
  }
}

export const propertyDisplayManager = new PropertyDisplayManager();
