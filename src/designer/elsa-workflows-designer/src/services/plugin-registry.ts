import 'reflect-metadata';
import {Container, Service} from "typedi";
import {SwitchPlugin} from "../plugins/switch/switch-plugin";
import {Plugin} from "../models";
import {SequencePlugin} from "../plugins/sequence/sequence-plugin";

// A registry of plugins.
@Service()
export class PluginRegistry {
  private readonly plugins: Array<Plugin> = [];

  constructor() {
    this.add(Container.get(SequencePlugin));
    this.add(Container.get(SwitchPlugin));
  }

  public add(plugin: Plugin) {
    this.plugins.push(plugin);
  }
}
