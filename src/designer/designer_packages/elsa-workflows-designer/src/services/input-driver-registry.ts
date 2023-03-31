import _ from 'lodash';
import {Service} from 'typedi';
import {ActivityInputDriver, ActivityInputContext} from './activity-input-driver';
import {DefaultInputDriver} from "../drivers/input/default-input-driver";

@Service()
export class InputDriverRegistry {
  private drivers: Array<ActivityInputDriver> = [new DefaultInputDriver()];

  public add(driver: ActivityInputDriver) {
    const drivers: Array<ActivityInputDriver> = [...this.drivers, driver]
    this.drivers = _.orderBy(drivers, x => x.priority, 'desc');
  }

  public get(context: ActivityInputContext): ActivityInputDriver {
    return this.drivers.find(x => x.supportsInput(context));
  }
}
