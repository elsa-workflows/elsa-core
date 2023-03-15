import {h} from '@stencil/core';
import { WarningIcon } from '../../icons/tooling';

export class DefaultContents {
    public static Warning = (message: string): any => {
        return(
            <div class="p-6 text-center">
                <WarningIcon/>
                <h3 class="mb-5 text-lg font-normal text-gray-500 dark:text-gray-400">{message}</h3>
            </div>
        )
    };
}