export interface DesignerGridOptions {
    type: string;
    visible: boolean;
    size: number;
    args: any;
}

export function createDesignerGridOptions(settings?: any): DesignerGridOptions {
    return {
        type: settings?.type ?? 'dot',
        visible: settings?.visible ?? true,
        size: settings?.size ?? 10,
        args: settings?.args,
    };
}

export function resolveDesignerGridOptions(options: DesignerGridOptions, themeColor: string) {
    return {
        type: options.type,
        visible: options.visible,
        size: options.size,
        args: resolveGridArgs(options.args, themeColor),
    };
}

function resolveGridArgs(args: any, themeColor: string) {
    if (Array.isArray(args))
        return args.map(item => ({...item, color: item?.color ?? themeColor}));

    return {...(args ?? {}), color: args?.color ?? themeColor};
}
