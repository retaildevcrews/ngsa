import { inject, injectable } from "inversify";
import { Controller, Get, interfaces } from "inversify-restify-utils";
import * as HttpStatus from "http-status-codes";
import { LogService } from "../services";
import { Movie } from "../models/Movie";
import { baseUrl, controllerExceptions } from "../config/constants";
import { getHttpStatusCode } from "../utilities/httpStatusUtilities";
const axios = require("axios").default;

// controller implementation for our featured movie endpoint
@Controller("/api/featured")
@injectable()
export class FeaturedController implements interfaces.Controller {

    private featuredMovies: string[];

    constructor(@inject("LogService") private logger: LogService) {}

    @Get("/movie")
    public async getFeaturedMovie(req, res) {
        let resCode: number = HttpStatus.OK;
        let result: Movie;
        let response: any;

        try {
            response = await axios.get(`${baseUrl}/api/featured/movie`);
            result = new Movie(response.data);
        } catch (err) {
            resCode = getHttpStatusCode(err);
            this.logger.error(Error(err), `${controllerExceptions.featuredControllerException}: ${err.toString()}`);
            return res.send(resCode, { status: resCode, message: controllerExceptions.featuredControllerException });
        }

        return res.send(resCode, result);
    }
}
