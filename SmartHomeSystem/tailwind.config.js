/** @type {import('tailwindcss').Config} */
module.exports = {
    darkMode: 'class', // or 'media'
    content: [
        "./Pages/**/*.cshtml",
        "./Views/**/*.cshtml",
        "./wwwroot/js/**/*.js",
        "./**/*.html"
    ],
    theme: {
        extend: {},
    },
    plugins: [],
};
