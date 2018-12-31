declare interface JQuery {
    notify(options: NotifyOptions): JQuery;
}

declare interface JQueryStatic {
    notify(options: NotifyOptions, settings: NotifySettings): JQuery;
}

interface NotifyOptions {
    icon?: string;
    title?: string;
    url?: string;
    target?: string;
    message: string;
}

interface NotifySettings {
    element?: string;
    position?: string;
    type?: string;
    allow_dismiss?: boolean;
    newest_on_top?: boolean;
    showProgressbar?: boolean;
    placement?: NotifyPlacement;
    offset?: number;
    spacing?: number;
    z_index?: number;
    delay?: number;
    timer?: number;
    url_target?: string;
    mouse_over?: any;
    animate?: NotifyAnimation;
    icon_type?: string;
    template?: string;
}

interface NotifyPlacement
{
    from: string;
    align: string;
}

interface NotifyAnimation
{
    enter: string;
    exit: string;
}