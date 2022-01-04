import 'reflect-metadata';
import {ActivityDriver, ActivityDriverFactory} from "./activity-driver";
import {Container, Service} from "typedi";
import {DefaultActivityDriver} from "../drivers/activity/default-activity-driver";

@Service()
export class ActivityDriverRegistry {
  private driverMap: Map<string, ActivityDriverFactory> = new Map<string, ActivityDriverFactory>();
  private defaultDriverFactory: ActivityDriverFactory = () => Container.get(DefaultActivityDriver);

  public add(activityType: string, driverFactory: ActivityDriverFactory) {
    this.driverMap.set(activityType, driverFactory);
  }

  public get(activityType: string): ActivityDriverFactory {
    return this.driverMap.get(activityType);
  }

  public createDriver(activityType: string): ActivityDriver {
    const driverFactory = this.get(activityType) ?? this.defaultDriverFactory;
    return driverFactory();
  }
}
