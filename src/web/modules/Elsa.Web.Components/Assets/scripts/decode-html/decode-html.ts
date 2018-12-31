module decode {
    const entities: any = {
        'amp': '&',
        'apos': '\'',
        'lt': '<',
        'gt': '>',
        'quot': '"',
        'nbsp': '\xa0'
    };

    const entityPattern: RegExp = /&([a-z]+);/ig;

    export function decodeHTMLEntities(text: string): string {
        return text.replace(entityPattern, function (match, entity) {
            entity = entity.toLowerCase();
            if (entities.hasOwnProperty(entity)) {
                return entities[entity];
            }
            return match;
        });
    };

}