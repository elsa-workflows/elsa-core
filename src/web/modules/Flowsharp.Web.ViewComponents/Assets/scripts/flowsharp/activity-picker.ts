///<reference path='../../node_modules/@types/jquery/index.d.ts' />
///<reference path='../../node_modules/@types/bootstrap/index.d.ts' />
///<reference path="selected-activity-info.ts"/>

namespace Flowsharp {
    export class ActivityPicker {

        private readonly container: JQuery<HTMLElement>;

        constructor(private containerElement: HTMLElement) {
            this.container = $(containerElement);
            this.container.find('a[data-activity-name]').on('click', this.onSelectActivityClick);

            this.container.find('.activity-picker-categories').on('click', '.nav-link', e => {
                this.applyFilter($(e.target).attr('href').substr(1), null);
            });

            this.container.find('.modal-activities input[type=search]').on('keyup', e => {
                this.applyFilter(null, <string>$(e.target).val());
            });

            this.container.on('show.bs.modal', (event) => {
                const button = $(event.relatedTarget); // Button that triggered the modal.
                const title = button.data('picker-title');
                const trigger = button.data('activity-trigger');
                const modal = this.container;
                modal.find('[href="#all"]').click();
                modal.find('.modal-title').text(title);
                modal.data('activity-trigger', trigger);
                this.applyFilter(null, null);
            })
        }

        public show = () => this.container.modal('show');
        public hide = () => this.container.modal('hide');

        public addEventListener = (eventName: string, listener: EventListenerOrEventListenerObject): void => {
            return this.container[0].addEventListener(eventName, listener);
        };

        private onSelectActivityClick = (e: JQuery.Event): void => {
            e.preventDefault();

            const selectedActivityInfo: SelectedActivityInfo = {
                activityName: $(e.target).data('activity-name'),
                activityDisplayText: $(e.target).data('activity-display-text')
            };
            const event = new CustomEvent('activity-selected', {detail: selectedActivityInfo});
            this.container[0].dispatchEvent(event);
        };

        private applyFilter = (category: string, q: string) => {
            const trigger = this.container.data('activity-trigger');
            category = category || $('.activity-picker-categories .nav-link.active').attr('href').substr(1);
            q = q || <string>this.container.find('input[type=search]').val();

            const $cards = $('.activity.card').show();

            // Remove activities whose type doesn't match the configured activity type.
            $cards.filter((i, el) => {
                return $(el).data('activity-trigger') != trigger;
            }).hide();

            if (q.length > 0) {
                // Remove activities whose title doesn't match the query.
                $cards.filter((i, el) => {
                    return $(el).find('.card-title').text().toLowerCase().indexOf(q.toLowerCase()) < 0 && q && q.length > 0;
                }).hide();
            } else {
                // Remove activities whose category doesn't match the selected one.
                $cards.filter((i, el) => {
                    return $(el).data('category').toLowerCase() != category.toLowerCase() && category.toLowerCase() != 'all';
                }).hide();
            }

            // Show or hide categories based on whether there are any available activities.
            $('.activity-picker-categories [data-category]').each((i, el) => {
                const categoryListItem = $(el);
                const category = categoryListItem.data('category');

                // Count number of activities within this category and for the specified activity type (Trigger or Action).
                const activityCount = $(`.activity.card[data-category='${category}'][data-activity-trigger='${trigger}']`).length;
                activityCount == 0 ? categoryListItem.hide() : categoryListItem.show();
            });
        };
    }
}