﻿import {ActivityModel, ActivityPropertyDescriptor, ElsaStudio} from "../models/";
import {PropertyDisplayDriver} from "./property-display-driver";
import {NullPropertyDriver } from "../drivers";
import {Map} from '../utils/utils';

export type PropertyDisplayDriverMap = Map<() => PropertyDisplayDriver>;

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

  addDriver<T extends PropertyDisplayDriver>(controlType: string, driver: T) {
    this.drivers[controlType] = () => driver;
  }

  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const driver = this.getDriver(property.uiHint);
    return driver.display(activity, property);
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
    const driver = this.getDriver(property.uiHint);
    return driver.update(activity, property, form);
  }

  getDriver(type: string) {
    const driverFactory = this.drivers[type] || ((_: ElsaStudio) => new NullPropertyDriver());
    return driverFactory(this.elsaStudio);
  }
}

export const propertyDisplayManager = new PropertyDisplayManager();
