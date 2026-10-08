import {getElement} from "./get-element";

export function getVisibleHeight(elementOrQuerySelector: Element | string): number {
    const element = getElement(elementOrQuerySelector);
    
    if(!element)
        return 0;
    
    const rect = element.getBoundingClientRect();
    const windowHeight = window.innerHeight;

    if (rect.bottom > windowHeight) {
        const visibleHeight = windowHeight - rect.top;
        return visibleHeight > 0 ? visibleHeight : 0;
    }

    return rect.height;
}