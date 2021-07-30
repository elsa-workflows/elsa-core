import i18next, {i18n} from 'i18next';
import 'i18next-wc';
import {h} from "@stencil/core";

export const IntlMessage = (props) => (<intl-message { ...{i18next, ...props }} />);

export function GetIntlMessage(i18next: i18n){
  return (props) => (<intl-message { ...{i18next, ...props }} />);
}