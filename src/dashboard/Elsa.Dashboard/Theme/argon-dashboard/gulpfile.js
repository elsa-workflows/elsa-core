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
    NPM: './node_modules/',
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

gulp.task('watch', function (done) {
    gulp.watch(Paths.SCSS, ['compile-scss']);
    done();
});

gulp.task('open', function () {
    gulp.src('examples/index.html')
        .pipe(open());
});

gulp.task('copy-styles', function (done) {
    gulp.src(`${Paths.ASSETS}css/**/*.*`)
        .pipe(gulp.dest(`${Paths.DIST}assets/css/`, {overwrite: true}));
    done();
});

gulp.task('copy-fonts', function (done) {
    gulp.src(`${Paths.ASSETS}fonts/**/*.*`)
        .pipe(gulp.dest(`${Paths.DIST}assets/fonts/`));
    done();
});

gulp.task('copy-images', function (done) {
    gulp.src(`${Paths.ASSETS}img/**/*.*`)
        .pipe(gulp.dest(`${Paths.DIST}assets/img/`, {overwrite: true}));
    done();
});

gulp.task('copy-scripts', function (done) {
    gulp.src(`${Paths.ASSETS}js/**/*.*`)
        .pipe(gulp.dest(`${Paths.DIST}assets/js/`, {overwrite: true}));
    done();
});

gulp.task('copy-scripts-npm', function (done) {
    gulp.src(`${Paths.NPM}@elsa-workflows/elsa-workflow-designer/dist/**/*.*`)
        .pipe(gulp.dest(`${Paths.DIST}assets/js/plugins/elsa-workflows/`, {overwrite: true}));
    done();
});

gulp.task('open-app', gulp.series('open', 'watch'));
gulp.task('build', gulp.series('compile-scss', 'copy-styles', 'copy-scripts', 'copy-scripts-npm', 'copy-fonts', 'copy-images'));
gulp.task('build-styles', gulp.series('compile-scss', 'copy-styles'));
