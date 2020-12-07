import "reflect-metadata";
import { ActorController, MovieController, FeaturedController, GenreController, HealthzController } from "./controllers";
import { BunyanLogService, LogService } from "./services";
import { Container } from "inversify";
import { ConsoleController } from "./config/ConsoleController";
import { interfaces, TYPE } from "inversify-restify-utils";
import { NGSAServer } from "./NGSAServer";
import NodeCache = require("node-cache");

// main
(async function main() {
    const container: Container = new Container();

    // setup logService (we need it for configuration)
    container.bind<LogService>("LogService").to(BunyanLogService).inSingletonScope();
    const logService = container.get<LogService>("LogService");
    
    // parse command line arguments to get the Key Vault url and auth type
    const consoleController = new ConsoleController(logService);
    await consoleController.run();
    const healthzCache = new NodeCache();

    // setup ioc container
    container.bind<NodeCache>("NodeCache").toConstantValue(healthzCache);
    container.bind<interfaces.Controller>(TYPE.Controller).to(ActorController).whenTargetNamed("ActorController");
    container.bind<interfaces.Controller>(TYPE.Controller).to(FeaturedController).whenTargetNamed("FeaturedController");
    container.bind<interfaces.Controller>(TYPE.Controller).to(GenreController).whenTargetNamed("GenreController");
    container.bind<interfaces.Controller>(TYPE.Controller).to(MovieController).whenTargetNamed("MovieController");
    container.bind<interfaces.Controller>(TYPE.Controller).to(HealthzController).whenTargetNamed("HealthzController");

    // instantiate the server
    const ngsaServer = new NGSAServer(container);

    // start the server
    ngsaServer.start();

    // graceful shutdown
    ["SIGINT", "SIGTERM", "SIGQUIT"].forEach(signal => process.on(signal, () => {
        console.info(`Received '${signal}', commencing graceful shutdown. Waiting for active requests to complete.`);
        ngsaServer.shutdown();
    }));

})()
