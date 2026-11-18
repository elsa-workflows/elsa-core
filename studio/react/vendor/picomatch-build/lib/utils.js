/*global navigator*/
'use strict';

const {
  REGEX_BACKSLASH,
  REGEX_SPECIAL_CHARS,
  REGEX_SPECIAL_CHARS_GLOBAL
} = require('./constants');

exports.isObject = val => val !== null && typeof val === 'object' && !Array.isArray(val);
exports.hasRegexChars = str => REGEX_SPECIAL_CHARS.test(str);
exports.isRegexChar = str => str.length === 1 && exports.hasRegexChars(str);
exports.escapeRegex = str => str.replace(REGEX_SPECIAL_CHARS_GLOBAL, '\\$1');
exports.toPosixSlashes = str => str.replace(REGEX_BACKSLASH, '/');

exports.isWindows = () => {
  if (typeof navigator !== 'undefined' && navigator.platform) {
    const platform = navigator.platform.toLowerCase();
    return platform === 'win32' || platform === 'windows';
  }

  if (typeof process !== 'undefined' && process.platform) {
    return process.platform === 'win32';
  }

  return false;
};

exports.removeBackslashes = str => {
  const output = [];
  const lineBreaks = /[\n\r\u2028\u2029]/g;
  const isLineBreak = /[\n\r\u2028\u2029]/;
  let lineEnd = -1;
  let bracketEnd = 0;

  for (let index = 0; index < str.length; index++) {
    const character = str[index];
    if (character === '[') {
      if (lineEnd < index) {
        lineBreaks.lastIndex = index + 1;
        const nextBreak = lineBreaks.exec(str);
        lineEnd = nextBreak ? nextBreak.index : str.length;
      }

      // Match the first closing bracket with a non-backslash predecessor.
      // Keep the search cursor across openings so each character is examined once.
      bracketEnd = Math.max(bracketEnd, index + 2);
      while (bracketEnd < str.length && (str[bracketEnd] !== ']' || str[bracketEnd - 1] === '\\')) {
        bracketEnd++;
      }

      if (bracketEnd < str.length && bracketEnd <= lineEnd + 1) {
        output.push(str.slice(index, bracketEnd + 1));
        index = bracketEnd;
        continue;
      }
    }

    if (character !== '\\' || index + 1 === str.length || isLineBreak.test(str[index + 1])) {
      output.push(character);
    }
  }

  return output.join('');
};

exports.escapeLast = (input, char, lastIdx) => {
  const idx = input.lastIndexOf(char, lastIdx);
  if (idx === -1) return input;
  if (input[idx - 1] === '\\') return exports.escapeLast(input, char, idx - 1);
  return `${input.slice(0, idx)}\\${input.slice(idx)}`;
};

exports.removePrefix = (input, state = {}) => {
  let output = input;
  if (output.startsWith('./')) {
    output = output.slice(2);
    state.prefix = './';
  }
  return output;
};

exports.wrapOutput = (input, state = {}, options = {}) => {
  const prepend = options.contains ? '' : '^';
  const append = options.contains ? '' : '$';

  let output = `${prepend}(?:${input})${append}`;
  if (state.negated === true) {
    output = `(?:^(?!${output}).*$)`;
  }
  return output;
};

exports.basename = (path, { windows } = {}) => {
  const segs = path.split(windows ? /[\\/]/ : '/');
  const last = segs[segs.length - 1];

  if (last === '') {
    return segs[segs.length - 2];
  }

  return last;
};
