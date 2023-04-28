import moment from 'moment';
import {v4 as uuid} from 'uuid';
import _, {camelCase} from 'lodash';
import {ActivityInput, VersionOptions} from '../models';
import {ActivityInputContext} from '../services/activity-input-driver';

export interface Hash<TValue> {
  [key: string]: TValue;
}

export function formatTimestamp(timestamp?: Date, defaultText?: string): string {
  return !!timestamp ? moment(timestamp).format('DD-MM-YYYY HH:mm:ss') : defaultText;
}

export function formatTime(timestamp?: Date, defaultText?: string): string {
  return !!timestamp ? moment(timestamp).format('HH:mm:ss') : defaultText;
}

export function getDuration(a: Date, b: Date): moment.Duration {
  const diff = moment(a).diff(moment(b));
  return moment.duration(diff);
}

export function getObjectOrParseJson(value: string | object) {
  return typeof value === 'string' ? parseJson(value) : value;
}

export function parseJson(json: string): any {
  if (!json)
    return null;

  if (json.trim().length === 0)
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
  return inputContext.activity[propName] as ActivityInput;
};

export const getPropertyValue = (inputContext: ActivityInputContext): any => {
  const propName = getInputPropertyName(inputContext);
  return inputContext.activity[propName] as any
};

export const stripActivityNameSpace = (name: string): string => {
  const lastDotIndex = name.lastIndexOf('.');
  return lastDotIndex < 0 ? name : name.substr(lastDotIndex + 1);
};

export const isNullOrWhitespace = (input) => !input || !input.trim();

export const serializeQueryString = (queryString: object): string => {
  const filteredItems = _(queryString).omitBy(_.isUndefined).omitBy(_.isNull).value();
  const queryStringItems = _.map(filteredItems, (v: any, k) => {
    if (Array.isArray(v)) {
      return v.map(item => `${k}=${item}`).join('&');
    }
    ;
    return `${k}=${v}`;
  });
  return queryStringItems.length > 0 ? `?${queryStringItems.join('&')}` : '';
};

export function durationToString(duration: moment.Duration) {
  return !!duration ? duration.asHours() > 1
      ? `${duration.asHours().toFixed(3)} h`
      : duration.asMinutes() > 1
        ? `${duration.asMinutes().toFixed(3)} m`
        : duration.asSeconds() > 1
          ? `${duration.asSeconds().toFixed(3)} s`
          : `${duration.asMilliseconds()} ms`
    : null;
}

export function htmlToElement<TElement>(html: string): TElement {
  const template = document.createElement('template');
  html = html.trim(); // Never return a text node of whitespace as the result
  template.innerHTML = html;
  return template.content.firstChild as unknown as TElement;
}

export function generateIdentity() {
  return uuid().replace(/-/g, '');
}

export function formatTextWithLineBreaks(text: string) {
  return text.replace(/\n/g, '<br />');
}
