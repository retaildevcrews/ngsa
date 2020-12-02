// eslint-disable-next-line @typescript-eslint/no-var-requires
const pkg = require("../../package.json");
// eslint-disable-next-line @typescript-eslint/no-var-requires
const swagger = require("../../swagger/helium.json");

import fs = require("fs");

export class VersionUtilities {

    // build and return the version string based on last build date time
    // build time based on dist/server.js file
    public static getBuildVersion(): string {
        
        // get the build time (i.e. 2020-04-02T05:11:04.483Z) and pull out the interesting parts
        const buildTime = fs.statSync("./dist/server.js").mtime.toISOString();
        const [, month, day, hour, minute] = /\d*-(\d*)-(\d*)T(\d*):(\d*):.*Z/.exec(buildTime);

        return `${pkg.version}+${month}${day}.${hour}${minute}`;
    }

    // return the version string based on swagger/helium.json file
    public static getSwaggerVersion(): string {
        return `${swagger.info.version}`;
    }
}
