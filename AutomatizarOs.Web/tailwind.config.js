/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
      "./**/*.{html, js, razor}"
  ],
  theme: {
    colors: {
      "primary": "#FFFFFF",
      "black": "#000000",
    },
    fontFamily: {
      sans: ['Open Sans', 'sans-serif'],
      titillium: ['Titillium Web', 'sans-serif']
    },
    extend: {},
  },
  plugins: [],
}

