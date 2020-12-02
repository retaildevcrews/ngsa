import { inject, injectable } from "inversify";
import { Controller, Get, interfaces } from "inversify-restify-utils";
import { Request } from "restify";
import * as HttpStatus from "http-status-codes";
import { LogService } from "../services";
import { baseUrl, controllerExceptions } from "../config/constants";
import { getHttpStatusCode } from "../utilities/httpStatusUtilities";
const axios = require("axios").default;

// controller implementation for our genres endpoint
@Controller("/api/genres")
@injectable()
export class GenreController implements interfaces.Controller {

    constructor(@inject("LogService") private logger: LogService) {}

    @Get("/")
    public async getAllGenres(req: Request, res) {
        let resCode: number = HttpStatus.OK;
        let results: string[];
        let response: any;

        try {
            response = await axios.get(`${baseUrl}/api/genres`);
            results = response.data;
        } catch (err) {
            resCode = getHttpStatusCode(err);
            this.logger.error(Error(err), `${controllerExceptions.genresControllerException}: ${err.toString()}`);
            return res.send(resCode, { status: resCode, message: controllerExceptions.genresControllerException });
        }

        return res.send(resCode, results);
    }
}
