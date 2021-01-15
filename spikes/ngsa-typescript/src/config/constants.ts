import { VersionUtilities } from "../utilities/versionUtilities";

export const baseUrl = "http://localhost:4122";

export const buildVersion = VersionUtilities.getBuildVersion();
export const swaggerVersion = VersionUtilities.getSwaggerVersion();
export const portConstant = "4120";
export const gracefulShutdownTimeout = 10000;
export const webInstanceRole = "WEBSITE_ROLE_INSTANCE_ID";
export const defaultPageSize = 100;

export const controllerExceptions = {
    actorsControllerException: "ActorsControllerException",
    featuredControllerException: "FeaturedControllerException",
    genresControllerException: "GenresControllerException",
    healthzControllerException: "HealthzControllerException",
    moviesControllerException: "MoviesControllerException"
};
