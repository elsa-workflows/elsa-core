import 'reflect-metadata';
import {Service} from "typedi";
import {Plugin} from "../../models";

@Service()
export class WorkflowInstancesPlugin implements Plugin {
  async initialize(): Promise<void> {

  }

}
