import { inject, injectable } from "inversify";
import { Controller, Get, interfaces } from "inversify-restify-utils";
import * as HttpStatus from "http-status-codes";
import { LogService } from "../services";
import { baseUrl, controllerExceptions, webInstanceRole, buildVersion } from "../config/constants";
import { DateUtilities } from "../utilities/dateUtilities";
import NodeCache = require("node-cache");
const axios = require("axios").default;

enum IetfStatus {
    pass = "pass",
    warn = "warn",
    fail = "fail",
}

// controller implementation for our healthz endpoint
@Controller("/healthz")
@injectable()
export class HealthzController implements interfaces.Controller {
 
    constructor(@inject("LogService") private logger: LogService,
                @inject("NodeCache") private cache: NodeCache) {
    }

    @Get("/")
    public async healthCheck(req, res) {
        // set Content-Type and Cache-Control headers
        res.setHeader("Content-Type", "text/plain");
        res.cache({maxAge: 60});

        let healthCheckResult: {[k: string]: any} = {};

        const cachedValue = this.cache.get("healthz");
        if (cachedValue != undefined) {
            healthCheckResult = cachedValue;
        } else {
            healthCheckResult = await this.runHealthChecks();
            this.cache.set("healthz", healthCheckResult, 60);
        }

        const resCode = healthCheckResult.status === IetfStatus.fail ? HttpStatus.SERVICE_UNAVAILABLE : HttpStatus.OK;

        return res.send(resCode, healthCheckResult.status);
    }

    @Get("/ietf")
    public async healthCheckIetf(req, res) {
        // set Content-Type and Cache-Control headers
        res.setHeader("Content-Type", "application/health+json");
        res.cache({maxAge: 60});

        let healthCheckResult: {[k: string]: any} = {};

        const cachedValue = this.cache.get("healthz");
        if (cachedValue != undefined) {
            healthCheckResult = cachedValue;
        } else {
            healthCheckResult = await this.runHealthChecks();
            this.cache.set("healthz", healthCheckResult, 60);
        }

        const resCode = healthCheckResult.status === IetfStatus.fail ? HttpStatus.SERVICE_UNAVAILABLE : HttpStatus.OK;

        res.writeHead(resCode, {
            "Content-Length": Buffer.byteLength(JSON.stringify(healthCheckResult)),
            "Content-Type": "application/health+json",
        });
        res.write(JSON.stringify(healthCheckResult));

        res.end();
    }

    // executes all health checks and builds the final ietf result
    private async runHealthChecks() {
        const ietfResult: {[k: string]: any} = {};
        ietfResult.status = IetfStatus.pass;
        ietfResult.serviceId =  "ngsa";
        ietfResult.description = "NGSA Typescript Health Check";
        ietfResult.instance = process.env[webInstanceRole] ?? "unknown";
        ietfResult.version = buildVersion;

        // declare health checks
        const healthChecks: {[k: string]: any} = {};
        const getGenres: {[k: string]: any} = {};
        const getActorById: {[k: string]: any} = {};
        const getMovieById: {[k: string]: any} = {};
        const searchMovies: {[k: string]: any} = {};
        const searchActors: {[k: string]: any} = {};

        try {
            healthChecks["getGenres:responseTime"] = getGenres;
            await this.runHealthCheck("getGenres", "/api/genres", 200, healthChecks["getGenres:responseTime"]);

            healthChecks["getActorById:responseTime"] = getActorById;
            await this.runHealthCheck("getActorById", "/api/actors/nm0000173", 100, healthChecks["getActorById:responseTime"]);

            healthChecks["getMovieById:responseTime"] = getMovieById;
            await this.runHealthCheck("getMovieById", "/api/movies/tt0133093", 100, healthChecks["getMovieById:responseTime"]);

            healthChecks["searchMovies:responseTime"] = searchMovies;
            await this.runHealthCheck("searchMovies", "/api/movies?q=ring", 200, healthChecks["searchMovies:responseTime"]);

            healthChecks["searchActors:responseTime"] = searchActors;
            await this.runHealthCheck("searchActors", "/api/actors?q=nicole", 200, healthChecks["searchActors:responseTime"]);

            // if any health check has a warn or down status
            // set overall status to the worst status
            for (const check in healthChecks) {
                if (healthChecks[check]) {
                    if (!(healthChecks[check].status === IetfStatus.pass)) {
                        ietfResult.status = healthChecks[check].status;
                    }

                    if (ietfResult.status === IetfStatus.fail) {
                        break;
                    }
                }
            }

            ietfResult.checks = healthChecks;
            return ietfResult;
        } catch (err) {
            this.logger.error(Error(err), `${controllerExceptions.healthzControllerException}: ${err.toString()}`);
            ietfResult.status = IetfStatus.fail;
            ietfResult.cosmosException = err.toString();
            ietfResult.checks = healthChecks;
            return ietfResult;
        }
    }

    // executes a health check and builds the result
    private async runHealthCheck(componentId: string, endpoint: string, target: number, healthCheckResult: any) {
        // start tracking time
        const startDate = new Date();
        const start = process.hrtime();

        // build health check result following ietf standard
        healthCheckResult.status = IetfStatus.pass;
        healthCheckResult.componentId = componentId;
        healthCheckResult.componentType = "datastore";
        healthCheckResult.observedUnit = "ms";
        healthCheckResult.observedValue = 0;
        healthCheckResult.targetValue = target;
        healthCheckResult.time = startDate.toISOString();

        try {
            // execute health check query based on endpoint
            await axios.get(`${baseUrl}${endpoint}`)

            // calculate duration in ms
            healthCheckResult.observedValue = DateUtilities.getDurationMS(process.hrtime(start));
        } catch (err) {
            // calculate duration
            // log exception and fail status, and re-throw exception
            healthCheckResult.observedValue = DateUtilities.getDurationMS(process.hrtime(start));
            healthCheckResult.status = IetfStatus.fail;
            healthCheckResult.affectedEndpoints = [ endpoint ];
            healthCheckResult.message = err.toString();

            throw err;
        }

        // set to warn if target duration is not met
        // only log affected endpoints if warn or fail status
        if (healthCheckResult.observedValue > healthCheckResult.targetValue) {
            healthCheckResult.status = IetfStatus.warn;
            healthCheckResult.affectedEndpoints = [ endpoint ];
            healthCheckResult.message = "Request exceeded expected duration";
        }
    }
}
