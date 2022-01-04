import _ from 'lodash';
import {Service} from 'typedi';
import {NodeInputDriver, NodeInputContext} from './node-input-driver';
import {DefaultInputDriver} from "../drivers/input/default-input-driver";

@Service()
export class InputDriverRegistry {
  private drivers: Array<NodeInputDriver> = [new DefaultInputDriver()];

  public add(driver: NodeInputDriver) {
    const drivers: Array<NodeInputDriver> = [...this.drivers, driver]
    this.drivers = _.orderBy(drivers, x => x.priority, 'desc');
  }

  public get(context: NodeInputContext): NodeInputDriver {
    return this.drivers.find(x => x.supportsInput(context));
  }
}
