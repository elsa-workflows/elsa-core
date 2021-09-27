// This is a modified copy of js-event-bus (https://github.com/bcerati/js-event-bus)/
// This modification adds support for async handlers.

export default function () {
  const that = this;
  this.listeners = {};

  this.registerListener = function (event, callback, number) {
    const type = event.constructor.name;
    number = this.validateNumber(number || 'any');

    if (type !== 'Array') {
      event = [event];
    }

    event.forEach(function (e) {
      if (e.constructor.name !== 'String') {
        throw new Error(
          'Only `String` and array of `String` are accepted for the event names!'
        );
      }

      that.listeners[e] = that.listeners[e] || [];
      that.listeners[e].push({
        callback: callback,
        number: number,
      });
    });
  };

  // validate that the number is a valid number for the number of executions
  this.validateNumber = function (n) {
    const type = n.constructor.name;

    if (type === 'Number')
      return n;
    else if (type === 'String' && n.toLowerCase() === 'any')
      return 'any';

    throw new Error(
      'Only `Number` and `any` are accepted in the number of possible executions!'
    );
  };

  // return whether or not this event needs to be removed
  this.toBeRemoved = function (info) {
    const number = info.number;
    info.execution = info.execution || 0;
    info.execution++;

    return !(number === 'any' || info.execution < number);


  };

  return {
    /**
     * Attach a callback to an event
     * @param {string} eventName - name of the event.
     * @param {function} callback - callback executed when this event is triggered
     */
    on(eventName, callback) {
      that.registerListener.bind(that)(eventName, callback, 'any');
    },

    /**
     * Attach a callback to an event. This callback will not be executed more than once if the event is trigger mutiple times
     * @param {string} eventName - name of the event.
     * @param {function} callback - callback executed when this event is triggered
     */
    once(eventName, callback) {
      that.registerListener.bind(that)(eventName, callback, 1);
    },

    /**
     * Attach a callback to an event. This callback will be executed will not be executed more than the number if the event is trigger mutiple times
     * @param {number} number - max number of executions
     * @param {string} eventName - name of the event.
     * @param {function} callback - callback executed when this event is triggered
     */
    exactly(number, eventName, callback) {
      that.registerListener.bind(that)(eventName, callback, number);
    },

    /**
     * Kill an event with all it's callbacks
     * @param {string} eventName - name of the event.
     */
    die(eventName) {
      delete that.listeners[eventName];
    },

    /**
     * Kill an event with all it's callbacks
     * @param {string} eventName - name of the event.
     */
    off(eventName) {
      this.die(eventName);
    },

    /**
     * Remove the callback for the given event
     * @param {string} eventName - name of the event.
     * @param {callback} callback - the callback to remove (undefined to remove all of them).
     */
    detach(eventName, callback) {
      if (callback === undefined) {
        that.listeners[eventName] = [];
        return true;
      }

      for (var k in that.listeners[eventName]) {
        if (
          that.listeners[eventName].hasOwnProperty(k) &&
          that.listeners[eventName][k].callback === callback
        ) {
          that.listeners[eventName].splice(k, 1);
          return this.detach(eventName, callback);
        }
      }

      return true;
    },

    /**
     * Remove all the events
     */
    detachAll() {
      for (const eventName in that.listeners) {
        if (that.listeners.hasOwnProperty(eventName))
          this.detach(eventName);
      }
    },

    /**
     * Emit the event
     * @param {string} eventName - name of the event.
     */
    async emit(eventName, context) {
      const listeners = [];
      for (const name in that.listeners) {
        if (that.listeners.hasOwnProperty(name)) {
          if (name === eventName) {
            //TODO: this lib should definitely use > ES5
            Array.prototype.push.apply(listeners, that.listeners[name]);
          }

          if (name.indexOf('*') >= 0) {
            let newName = name.replace(/\*\*/, '([^.]+.?)+');
            newName = newName.replace(/\*/g, '[^.]+');

            const match = eventName.match(newName);
            if (match && eventName === match[0]) {
              Array.prototype.push.apply(listeners, that.listeners[name]);
            }
          }
        }
      }

      const parentArgs = arguments;

      context = context || this;
      let index = 0;
      for (const info of listeners) {

        let callback = info.callback;
        const number = info.number;

        if (context) {
          callback = callback.bind(context);
        }

        const args = [];
        Object.keys(parentArgs).map(function (i) {
          if (i > 1) {
            args.push(parentArgs[i]);
          }
        });

        // this event cannot be fired again, remove from the stack
        if (that.toBeRemoved(info)) {
          that.listeners[eventName].splice(index, 1);
        }

        const result = callback.apply(null, args);

        if (!!result)
          await result;

        index++;
      }
    },
  };
};
