import {h} from '@stencil/core';
import { WarningIcon } from '../../icons/tooling';

export class DefaultContents {
    public static Warning = (message: string): any => {
        return(
            <div class="tw-p-6 tw-text-center">
                <WarningIcon/>
                <h3 class="tw-mb-5 tw-text-lg tw-font-normal tw-text-gray-500 dark:tw-text-gray-400">{message}</h3>
            </div>
        )
    };
}
