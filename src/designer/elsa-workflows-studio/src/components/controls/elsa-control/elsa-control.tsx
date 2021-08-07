import {Component, h, Host, Prop, VNode} from '@stencil/core';

@Component({
    tag: 'elsa-control',
    shadow: false,
})
export class ElsaControl {
    @Prop() content: VNode | string | Element;
    el: HTMLElement;
    
    render() {
        const content: any = this.content;
        
        if(typeof content === 'string')
            return <Host innerHTML={content}/>;
        
        if(!!content.tagName)
            return <Host ref={el => this.el = el}/>;
        
        return (
            <Host>{content}</Host>
        );
    }
    
    componentDidLoad(){
        if(!this.el)
            return;
        
        const content = this.content as HTMLElement;
        this.el.append(content);
    }
}
