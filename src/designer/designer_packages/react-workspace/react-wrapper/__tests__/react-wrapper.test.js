'use strict';

const reactWrapper = require('..');
const assert = require('assert').strict;

assert.strictEqual(reactWrapper(), 'Hello from reactWrapper');
console.info('reactWrapper tests passed');
