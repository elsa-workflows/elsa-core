export function getElement(elementOrQuerySelector: Element | string): Element {
    return typeof elementOrQuerySelector === 'string' ? document.querySelector(elementOrQuerySelector) : elementOrQuerySelector;
}