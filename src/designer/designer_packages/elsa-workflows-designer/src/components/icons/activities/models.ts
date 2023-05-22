export enum ActivityIconSize {
  Small,
  Medium,
  Large
}

export interface ActivityIconSettings {
  size?: ActivityIconSize;
}

export function getActivityIconCssClass(settings: ActivityIconSettings) {
  const cssClasses = ['tw-text-white', sizeToClass(settings.size)];
  return cssClasses.join(' ');
}

function sizeToClass(size?: ActivityIconSize) {
  switch (size) {
    case ActivityIconSize.Small:
      return 'tw-h-4 tw-w-4';
    case ActivityIconSize.Medium:
      return 'tw-h-6 tw-w-6';
    case ActivityIconSize.Large:
      return 'tw-h-10 tw-w-10';
    default:
      return 'tw-h-6 tw-w-6';
  }
}
