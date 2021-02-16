import {ActivityModel, ActivityPropertyDescriptor} from "../models/";
import {PropertyDisplayDriver} from "./property-display-driver";
import {NullPropertyDriver, TextPropertyDriver} from "../property-display-drivers";
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
    const driverFactory = this.drivers[type] || (() => new NullPropertyDriver());
    return driverFactory();
  }
}

export const propertyDisplayManager = new PropertyDisplayManager();
