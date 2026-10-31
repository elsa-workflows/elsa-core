import {getElement} from "./get-element";

export function getBoundingClientRect(elementOrQuerySelector: Element | string): DOMRect {
    const element = getElement(elementOrQuerySelector);
    return element.getBoundingClientRect();
}