import * as restify from "restify";
import { Container } from "inversify";
import { LogService } from "../services/LogService";

// adds failure logs to every endpoint
export default function responseDuration(container: Container) {
    // get the log client
    const log: LogService = container.get<LogService>("LogService");

    // return a function with the correct middleware signature
    return function responseStatus(req: restify.Request, res: restify.Response, next) {

        // hook into response finish to log call result
        res.on("finish", (() => {
            if (res.statusCode > 399) {
                // create string unique to this action at this endpoint
                const apiName = `${req.method} ${req.url}`;
                log.warn(`${apiName} Result: ${res.statusCode}, ${req.getId()}`);
            }
        }));

        // call next middleware
        next();
    };
}
