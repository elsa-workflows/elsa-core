import {
  Activity,
  ActivityPropertyDescriptor,
  RenderResult
} from "../modelsCopy";
import { FieldDriver } from "./field-driver";
import { FieldDriverMap } from "../modelsCopy/field-driver-map";

export class DisplayManager {
  constructor() {
    this.drivers = {};
  }

  private readonly drivers: FieldDriverMap;

  public addDriver = (fieldType: string, driver: FieldDriver) => {
    this.drivers[fieldType] = { ...driver };
  };

  public displayEditor = (activity: Activity, property: ActivityPropertyDescriptor): RenderResult => {
    const driver = this.drivers[property.type];

    if (!driver)
      return null;

    return driver.displayEditor(activity, property);
  };

  public updateEditor = (activity: Activity, property: ActivityPropertyDescriptor, formData: FormData) => {
    const driver = this.drivers[property.type];

    if (!driver)
      return;

    driver.updateEditor(activity, property, formData);
  }
}

export default new DisplayManager();
