import moment from 'moment';
import {camelCase} from 'lodash';
import {ActivityInput, VersionOptions} from '../models';
import {NodeInputContext} from '../services/node-input-driver';

export function formatTimestamp(timestamp?: Date, defaultText?: string): string {
  return !!timestamp ? moment(timestamp).format('DD-MM-YYYY HH:mm:ss') : defaultText;
}

export function parseJson(json: string): any {
  if (!json)
    return null;

  try {
    return JSON.parse(json);
  } catch (e) {
    console.warn(`Error parsing JSON: ${e}`);
  }
  return undefined;
}

export const getVersionOptionsString = (versionOptions?: VersionOptions) => {

  if (!versionOptions)
    return '';

  return versionOptions.allVersions
    ? 'AllVersions'
    : versionOptions.isDraft
      ? 'Draft'
      : versionOptions.isLatest
        ? 'Latest'
        : versionOptions.isPublished
          ? 'Published'
          : versionOptions.isLatestOrPublished
            ? 'LatestOrPublished'
            : versionOptions.version.toString();
};

export const getInputPropertyName = (inputContext: NodeInputContext) => {
  const inputProperty = inputContext.inputDescriptor;
  const propertyName = inputProperty.name;
  return camelCase(propertyName);
}

export const getInputPropertyValue = (inputContext: NodeInputContext): ActivityInput => {
  const propName = getInputPropertyName(inputContext);
  return inputContext.node[propName] as ActivityInput;
};

export const setInputPropertyValue = (inputContext: NodeInputContext, value: any) => {
  const propName = getInputPropertyName(inputContext);
  return inputContext.node[propName] = value;
};
