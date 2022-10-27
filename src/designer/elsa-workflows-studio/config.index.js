#!/usr/bin/env node
const fs = require('fs');

const file = 'src/index.html';
const default_api_url = 'https://localhost:11000';

if(process.env.CODESPACE_NAME) {

    const new_api_url = `https://${process.env.CODESPACE_NAME}-11000.preview.app.github.dev/`;

    fs.readFile(file, 'utf8', (err, data) => {
        if(err) {
            return console.log(err);
        }

        const result =
          data.replace(default_api_url, new_api_url);

          fs.writeFile(file, result, 'utf8', function(err) {
            if (err) return console.log(err);
          });
    });

}

