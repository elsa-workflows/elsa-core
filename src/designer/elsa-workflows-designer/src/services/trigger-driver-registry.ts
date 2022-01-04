import {TriggerDriver, TriggerDriverFactory} from "./trigger-driver";
import {Service} from "typedi";
import {DefaultTriggerDriver} from "../drivers/trigger/default-trigger-driver";

@Service()
export class TriggerDriverRegistry {
  private _driverMap: Map<string, TriggerDriverFactory> = new Map<string, TriggerDriverFactory>();
  private _defaultDriverFactory: TriggerDriverFactory = () => new DefaultTriggerDriver();

  public add(triggerType: string, driverFactory: TriggerDriverFactory) {
    this._driverMap.set(triggerType, driverFactory);
  }

  public get(triggerType: string): TriggerDriverFactory {
    return this._driverMap.get(triggerType);
  }

  public createDriver(triggerType: string): TriggerDriver {
    const driverFactory = this.get(triggerType) ?? this._defaultDriverFactory;
    return driverFactory();
  }
}
