import { inject, injectable } from "inversify";
import { Controller, Get, interfaces } from "inversify-restify-utils";
import { Request } from "restify";
import * as HttpStatus from "http-status-codes";
import { LogService } from "../services";
import { Actor } from "../models/Actor";
import { baseUrl, controllerExceptions } from "../config/constants";
import { getHttpStatusCode, APIValidationUtilities } from "../utilities";
const axios = require("axios").default;

// controller implementation for our actors endpoint
@Controller("/api/actors")
@injectable()
export class ActorController implements interfaces.Controller {

    constructor(@inject("LogService") private logger: LogService) {}

    @Get("/")
    public async getAllActors(req: Request, res) {
        // validate query parameters
        const { validated: validated, errorResponse: errorResponse } = APIValidationUtilities.validateActors(req.query, req.path(), req.getQuery());
        if (!validated) {
            this.logger.warn(`InvalidParameter|getAllActors|${errorResponse.detail}`);
            res.writeHead(HttpStatus.BAD_REQUEST, {
                "Content-Type": "application/problem+json",
            });
            res.write(JSON.stringify(errorResponse,null,4));
    
            return res.end();
        }

        let resCode: number = HttpStatus.OK;
        let results: Actor[];
        let response: any;

        try {
            response = await axios.get(`${baseUrl}/api/actors`, {params: req.query});
            results = response.data;
        } catch (err) {
            resCode = getHttpStatusCode(err);
            this.logger.error(Error(err), `${controllerExceptions.actorsControllerException}: ${err.toString()}`);
            return res.send(resCode, { status: resCode, message: controllerExceptions.actorsControllerException });
        }

        return res.send(resCode, results);
    }

    @Get("/:id")
    public async getActorById(req, res) {
        // validate Actor Id parameter
        const actorId: string = req.params.id;
        const { validated: validated, errorResponse: errorResponse } = APIValidationUtilities.validateActorId(actorId, req.path(), req.getQuery());

        if (!validated) {
            this.logger.warn(`getActorById|${actorId}|${errorResponse.detail}`);
            res.writeHead(HttpStatus.BAD_REQUEST, {
                "Content-Type": "application/problem+json",
            });
            res.write(JSON.stringify(errorResponse,null,4));
    
            return res.end();
        }

        let resCode: number = HttpStatus.OK;
        let result: Actor;
        let response: any;

        try {
            response = await axios.get(`${baseUrl}/api/actors/${actorId}`);
            result = new Actor(response.data);
        } catch (err) {
            resCode = getHttpStatusCode(err);

            if (resCode === HttpStatus.NOT_FOUND) {
                this.logger.warn(`Actor Not Found: ${actorId}`);
                return res.send(resCode, {status: resCode, message: "Actor Not Found"});
            }

            this.logger.error(Error(err), `${controllerExceptions.actorsControllerException}: ${err.toString()}`);
            return res.send(resCode, { status: resCode, message: controllerExceptions.actorsControllerException });
        }

        return res.send(resCode, result);
    }
}
