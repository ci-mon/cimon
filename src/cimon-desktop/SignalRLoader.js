module.exports = function (source) {
    const resourcePath = this.resourcePath;
    const logger = this.getLogger('ShitLoader');
    logger.warn(`Removing __non_webpack_require__ checks from ${resourcePath}`);
    return source.replaceAll('__non_webpack_require__', 'require');
};
