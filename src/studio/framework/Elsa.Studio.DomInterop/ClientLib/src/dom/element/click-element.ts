import {getElement} from "./get-element";

export function clickElement(elementOrQuerySelector: Element | string) {
    const element = getElement(elementOrQuerySelector) as HTMLElement;
    
    element.click();
}