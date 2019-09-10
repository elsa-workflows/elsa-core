var gulp = require('gulp');
var path = require('path');
var sass = require('gulp-sass');
var autoprefixer = require('gulp-autoprefixer');
var sourcemaps = require('gulp-sourcemaps');
var open = require('gulp-open');

var Paths = {
    HERE: './',
    DIST: '../../wwwroot/',
    ASSETS: './assets/',
    CSS: './assets/css/',
    SCSS_TOOLKIT_SOURCES: './assets/scss/**/**',
    SCSS: './assets/scss/**/**'
};

gulp.task('compile-scss', function () {
    return gulp.src(Paths.SCSS_TOOLKIT_SOURCES)
        .pipe(sourcemaps.init())
        .pipe(sass().on('error', sass.logError))
        .pipe(autoprefixer())
        .pipe(sourcemaps.write(Paths.HERE))
        .pipe(gulp.dest(Paths.CSS));
});

gulp.task('watch', function () {
    gulp.watch(Paths.SCSS, ['compile-scss']);
});

gulp.task('open', function () {
    gulp.src('examples/index.html')
        .pipe(open());
});

gulp.task('copy-styles', function () {
    gulp.src(`${Paths.ASSETS}css/**/*.*`)
        .pipe(gulp.dest(`${Paths.DIST}assets/css/`, {overwrite: true}));
});

gulp.task('copy-fonts', function () {
    gulp.src(`${Paths.ASSETS}fonts/**/*.*`)
        .pipe(gulp.dest(`${Paths.DIST}assets/fonts/`));
});

gulp.task('copy-images', function () {
    gulp.src(`${Paths.ASSETS}img/**/*.*`)
        .pipe(gulp.dest(`${Paths.DIST}assets/img/`, {overwrite: true}));
});

gulp.task('copy-scripts', function () {
    gulp.src(`${Paths.ASSETS}js/**/*.*`)
        .pipe(gulp.dest(`${Paths.DIST}assets/js/`, {overwrite: true}));
});

gulp.task('open-app', ['open', 'watch']);
gulp.task('build', ['compile-scss', 'copy-styles', 'copy-scripts', 'copy-fonts', 'copy-images']);
gulp.task('build-styles', ['compile-scss', 'copy-styles']);
