/// <binding BeforeBuild='pre-build, strip-typescript-reference-paths' AfterBuild='build-typescript, post-build' Clean='clean-dist' />

require('babel-polyfill');

var babel = require('gulp-babel');
var bundler = require('aurelia-bundler');
var del = require('del');
var gulp = require('gulp');
var runSequence = require('run-sequence');
var sourcemaps = require('gulp-sourcemaps');
var strip = require('gulp-strip-comments');
var typescript = require('gulp-typescript');

var config = {
    force: true,
    baseURL: './wwwroot',
    configPath: './wwwroot/config.js',
    bundles: {
        'dist/app-build': {
            includes: [
                '*.js',
                '*.html!text',
                '*.css!text',
                'bootstrap/css/bootstrap.css!text'
            ],
            options: {
                inject: true,
                minify: true
            }
        },

        'dist/aurelia': {
            includes: [
                'aurelia-bootstrapper',
                'aurelia-event-aggregator',
                'aurelia-fetch-client',
                'aurelia-framework',
                'aurelia-history-browser',
                'aurelia-loader-default',
                'aurelia-logging-console',
                'aurelia-router',
                'aurelia-templating-binding',
                'aurelia-templating-resources',
                'aurelia-templating-router',
                'aurelia-http-client',
                'aurelia-polyfills'
            ],

            options: {
                'inject': true,
                'minify': true,
                'depCache': true
            }
        }
    }
};

gulp.task('copy-apps', function () {
    return gulp.src('../../Apps/**/Web/**/*').pipe(gulp.dest('wwwroot/dist/Apps/'));
});

gulp.task('copy-sitecommon', function () {
    return gulp.src('../../SiteCommon/Web/**/*').pipe(gulp.dest('wwwroot/dist/SiteCommon/Web'));
});

gulp.task('copy-src', function () {
    return gulp.src('src/**/*').pipe(gulp.dest('wwwroot/dist/'));
});

gulp.task('clean-dist', function () {
    return del(['wwwroot/dist']);
});

gulp.task('pre-build', function (callback) {
    return runSequence('clean-dist', ['copy-apps', 'copy-sitecommon', 'copy-src'], callback);
});

gulp.task('strip-typescript-reference-paths', function () {
    return gulp.src('wwwroot/dist/**/*.ts').pipe(strip.text({ trim: true })).pipe(gulp.dest('wwwroot/dist'));
});

gulp.task('build-typescript', function () {
    return gulp.src(['wwwroot/**/*.ts', 'typings/**.*'])
        //.pipe(sourcemaps.init({ loadMaps: true }))
        .pipe(typescript({
            alwaysStrict: true,
            emitDecoratorMetadata: true,
            experimentalDecorators: true,
            forceConsistentCasingInFileNames: true,
            noImplicitAny: true,
            noImplicitReturns: true,
            noImplicitThis: true,
            noUnusedLocals: true,
            noUnusedParameters: true,
            removeComments: true,
            //sourceMap: true,
            target: 'es6'
        }))
        .pipe(babel({
            plugins: ['transform-runtime'],
            presets: ['es2015']
        }))
        //.pipe(sourcemaps.write('.', {
        //    includeContent: false,
        //    sourceRoot: '/'
        //}))
        .pipe(gulp.dest('wwwroot/'));
});

gulp.task('post-build', function () {
    return bundler.bundle(config);
    //return bundler.unbundle(config);
});