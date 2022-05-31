import {Service} from 'typedi';
import {camelCase, startCase, snakeCase, kebabCase} from 'lodash';
import {ActivityDescriptor} from "../models";
import {ActivityNode} from "../components/activities/flowchart/activity-walker";
import {stripActivityNameSpace} from "../utils";

export type ActivityNameStrategy = (context: ActivityNameFormatterContext) => string;

export interface ActivityNameFormatterContext {
  activityDescriptor: ActivityDescriptor;
  count: number;
  activityNodes: Array<ActivityNode>;
}

@Service()
export class ActivityNameFormatter {

  public static readonly DefaultStrategy: ActivityNameStrategy = context => `${stripActivityNameSpace(context.activityDescriptor.activityType)}${context.count}`;
  public static readonly UnderscoreStrategy: ActivityNameStrategy = context => `${stripActivityNameSpace(context.activityDescriptor.activityType)}_${context.count}`;
  public static readonly PascalCaseStrategy: ActivityNameStrategy = context => startCase(camelCase(ActivityNameFormatter.DefaultStrategy(context))).replace(/ /g, '');
  public static readonly CamelCaseStrategy: ActivityNameStrategy = context => camelCase(ActivityNameFormatter.DefaultStrategy(context));
  public static readonly SnakeCaseStrategy: ActivityNameStrategy = context => snakeCase(ActivityNameFormatter.DefaultStrategy(context));
  public static readonly KebabCaseStrategy: ActivityNameStrategy = context => kebabCase(ActivityNameFormatter.DefaultStrategy(context));

  public strategy: ActivityNameStrategy = ActivityNameFormatter.PascalCaseStrategy;

  public format(context: ActivityNameFormatterContext): string {
    return this.strategy(context);
  }
}
