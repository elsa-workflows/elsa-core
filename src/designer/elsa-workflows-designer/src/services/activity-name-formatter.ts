import {Service} from 'typedi';
import {camelCase, startCase, snakeCase, kebabCase} from 'lodash';
import {Activity, ActivityDescriptor} from "../models";
import {stripActivityNameSpace} from "../utils";
import {ActivityNode} from "./activity-walker";

export type ActivityNameStrategy = (context: ActivityNameFormatterContext) => string;

export interface ActivityNameFormatterContext {
  activityDescriptor: ActivityDescriptor;
  count: number;
  activities: Array<Activity>;
}

@Service()
export class ActivityNameFormatter {

  public static readonly DefaultStrategy: ActivityNameStrategy = context => `${stripActivityNameSpace(context.activityDescriptor.typeName)}${context.count}`;
  public static readonly UnderscoreStrategy: ActivityNameStrategy = context => `${stripActivityNameSpace(context.activityDescriptor.typeName)}_${context.count}`;
  public static readonly PascalCaseStrategy: ActivityNameStrategy = context => startCase(camelCase(ActivityNameFormatter.DefaultStrategy(context))).replace(/ /g, '');
  public static readonly CamelCaseStrategy: ActivityNameStrategy = context => camelCase(ActivityNameFormatter.DefaultStrategy(context));
  public static readonly SnakeCaseStrategy: ActivityNameStrategy = context => snakeCase(ActivityNameFormatter.DefaultStrategy(context));
  public static readonly KebabCaseStrategy: ActivityNameStrategy = context => kebabCase(ActivityNameFormatter.DefaultStrategy(context));

  public strategy: ActivityNameStrategy = ActivityNameFormatter.PascalCaseStrategy;

  public format(context: ActivityNameFormatterContext): string {
    return this.strategy(context);
  }
}
