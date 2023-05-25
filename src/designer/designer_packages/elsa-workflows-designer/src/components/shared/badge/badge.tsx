import {FunctionalComponent, h} from "@stencil/core";
import { TinyColor } from '@ctrl/tinycolor';

export interface BadgeProps {
  text?: string;
  color?: string;
}

export const Badge: FunctionalComponent<BadgeProps> = ({text, color}) => {
  const foreColor = color;
  const backColor = new TinyColor(color).lighten(55).toHexString();

  const style = {
    color: foreColor,
    backgroundColor: backColor
  };

  return <span class="tw-inline-flex tw-items-center tw-px-3 tw-py-0.5 tw-rounded-full tw-text-sm tw-font-medium" style={style}>{text}</span>;
};
