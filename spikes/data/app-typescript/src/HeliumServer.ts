import "reflect-metadata";
import EndpointLogger from "./middleware/EndpointLogger";
import { LogService } from "./services";
import { Container } from "inversify";
import { InversifyRestifyServer } from "inversify-restify-utils";
import { html } from "./swagger-html";
import { robotsHandler } from "./middleware/robotsText";
import { buildVersion, swaggerVersion, gracefulShutdownTimeout, portConstant } from "./config/constants";
import bodyParser = require("body-parser");
import restify = require("restify");

export class HeliumServer {
    private server: restify.Server;
    private inversifyServer: InversifyRestifyServer;
    private logService: LogService;

    constructor(private container: Container) {
        this.inversifyServer = new InversifyRestifyServer(this.container);
        this.logService = this.container.get<LogService>("LogService");
        this.server = this.createRestifyServer();

        this.logService.info(`Build Version: ${buildVersion}`);
    }

    createRestifyServer() {
        return this.inversifyServer.setConfig(app => {
            // middleware
            app
                .use(bodyParser.urlencoded({ extended: true }))
                .pre(robotsHandler)
                .use(restify.plugins.queryParser({ mapParams: false }))
                .use(bodyParser.json())
                .use(restify.plugins.requestLogger())
                .use(EndpointLogger(this.container));

            // routes
            app.get("/swagger/*", restify.plugins.serveStatic({
                directory: __dirname + "/..",
                default: "helium.json",
            }));

            app.get("/", (req, res) => {
                res.writeHead(200, {
                    "Content-Length": Buffer.byteLength(html),
                    "Content-Type": "text/html",
                });
                res.write(html);
                res.end();
            });

            app.get("/node_modules/swagger-ui-dist/*", restify.plugins.serveStatic({
                directory: __dirname + "/..",
            }));

            app.get("/version", (req, res) => {
                res.setHeader("Content-Type", "application/json");
                res.send({
                    apiVersion: swaggerVersion,
                    appVersion: buildVersion,
                    language: "typescript"
                });
            });
        }).build();
    }

    public start() {
        this.server.listen(portConstant, () => {
            this.logService.info(`Server is listening on port ${portConstant}`);
        });
    }

    public shutdown() {
        if (!this.server) {
            console.info("Server not defined. Exiting.");
            process.exit(0);
        }

        this.server.close(() => {
            console.info("Graceful Shutdown complete.");
            process.exit(0);
        });

        // allow existing requests to be processed until timeout, then force shutdown
        setTimeout(() => {
            console.info("Graceful shutdown aborted with one or more requests still active.");
            process.exit(0);
        }, gracefulShutdownTimeout);
    }
}
