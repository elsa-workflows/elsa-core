import moment from 'moment';
import {camelCase} from 'lodash';
import {ActivityInput, VersionOptions} from '../models';
import {ActivityInputContext} from '../services/node-input-driver';

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

export const mapSyntaxToLanguage = (syntax: string): string => {
  switch (syntax) {
    case 'Json':
      return 'json';
    case 'JavaScript':
      return 'javascript';
    case 'Liquid':
      return 'handlebars';
    default:
      return 'plaintext';
  }
};

export const getInputPropertyName = (inputContext: ActivityInputContext) => {
  const inputProperty = inputContext.inputDescriptor;
  const propertyName = inputProperty.name;
  return camelCase(propertyName);
}

export const getInputPropertyValue = (inputContext: ActivityInputContext): ActivityInput => {
  const propName = getInputPropertyName(inputContext);
  return inputContext.node[propName] as ActivityInput;
};

export const stripActivityNameSpace = (name: string): string => {
  const lastDotIndex = name.lastIndexOf('.');
  return lastDotIndex < 0 ? name : name.substr(lastDotIndex + 1);
};

export const isNullOrWhitespace = (input) => !input || !input.trim();
