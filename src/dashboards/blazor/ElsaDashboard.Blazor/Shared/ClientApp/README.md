# Tailwind CSS Playground

A simple starter project for playing around with Tailwind in a proper PostCSS environment.

To get started:

1. Clone the repository:

   ```bash
   git clone https://github.com/tailwindcss/playground.git tailwindcss-playground

   cd tailwindcss-playground
   ```

2. Install the dependencies:

   ```bash
   # Using npm
   npm install

   # Using Yarn
   yarn
   ```

3. Start the development server:

   ```bash
   # Using npm
   npm run serve

   # Using Yarn
   yarn serve
   ```

   Now you should be able to see the project running at localhost:8080.

4. Open `public/index.html` in your editor and start experimenting!

## Play with it on CodeSandbox

A CodeSandbox version of this playground [is one click away](https://codesandbox.io/s/github/tailwindcss/playground/tree/master)!

## Building for production

Even though this isn't necessarily a starter kit for a proper project, we've included an example of setting up [cssnano](https://cssnano.co/) to optimize your CSS for production.

To build an optimized version of your CSS, simply run:

```bash
# Using npm
npm run production

# Using Yarn
yarn production
```

After that's done, check out `./public/build/tailwind.css` to see the optimized output.
