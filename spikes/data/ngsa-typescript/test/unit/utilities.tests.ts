import { assert } from "chai";
import * as HttpStatus from "http-status-codes";
import { DateUtilities, VersionUtilities, APIValidationUtilities, queryErrorMessages, getHttpStatusCode } from "../../src/utilities";
import { ConsoleController } from "../../src/config/ConsoleController";
import { Container } from "inversify";
import { LogService, ConsoleLogService } from "../../src/services";

let logService;

const querySuffix = "test";
const queryPrefix = "/api/test";
describe("Utilities tests", () => {
  before(() => {
    const container: Container = new Container();
    container.bind<LogService>("LogService").to(ConsoleLogService);
    logService = container.get<LogService>("LogService");
  });

  describe("DateUtilities", () => {
    describe("getTimer", () => {
      it("should return correct type", () => {
        assert.typeOf(DateUtilities.getTimer(), "function");
      });
      it("should time for a select duration accurately enough", () => {
        const timer = DateUtilities.getTimer();
        const VALUE = 2000;
        let result;
        setTimeout(() => {
          result = timer();

          // make sure the timer result is within 5% of the target
          assert.isTrue(((result / VALUE) - 1) < 0.05);
        }, VALUE);
      })
    });
    describe("getDurationMS", () => {
      it("should return correct type", () => {
        assert.typeOf(DateUtilities.getDurationMS([1800216, 25]), "string");
      });
      it("should return correct duration", () => {
        assert.equal(DateUtilities.getDurationMS([1800216, 25]), "1800216000");
      });
    });
  });

  describe("VersionUtilities", () => {
    describe("getBuildVersion", () => {
      it("should return correct type", () => {
        assert.typeOf(VersionUtilities.getBuildVersion(), "string");
      });
    });
    describe("getSwaggerVersion", () => {
      it("should return correct type", () => {
        assert.typeOf(VersionUtilities.getSwaggerVersion(), "string");
      });
    });
  });

  describe("APIValidationUtilities", () => {
    describe("validateMovieId", () => {
      it("should invalidate ( null )", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovieId( null, queryPrefix, querySuffix );
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidMovieIDMessage);
      });

      it("should invalidate ( undefined )", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovieId( undefined, queryPrefix, querySuffix );
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidMovieIDMessage);
      });

      it("should validate movie ID tt333344", () => {
        const { validated } = APIValidationUtilities.validateMovieId("tt333344", queryPrefix, querySuffix);
        assert.isTrue(validated);
      });

      it("should invalidate TT333344 (uppercase prefix)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovieId("TT333344", queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidMovieIDMessage);
      });

      it("should invalidate nm333344 (incorrect prefix)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovieId("nm333344", queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidMovieIDMessage);
      });

      it("should invalidate tt (too short)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovieId("tt", queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidMovieIDMessage);
      });

      it("should invalidate tttttttttttt (too long)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovieId("tttttttttttt", queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidMovieIDMessage);
      });

      it("should invalidate ttabcdef (non-numeric after first 2 characters)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovieId("ttabcdef", queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidMovieIDMessage);
      });
    });

    describe("validateActorId", () => {
      it("should validate nm333344", () => {
        const { validated } = APIValidationUtilities.validateActorId("nm333344", queryPrefix, querySuffix);
        assert.isTrue(validated);
      });

      it("should invalidate NM333344 (upper case)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActorId("NM333344", queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidActorIDMessage);
      });

      it("should invalidate tt333344 (incorrect prefix)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActorId("tt333344", queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidActorIDMessage);
      });

      it("should invalidate nm (too short)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActorId("nm", queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidActorIDMessage);
      });

      it("should invalidate tttttttttttt (too long)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActorId("tttttttttttt", queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidActorIDMessage);
      });

      it("should invalidate nmabcdef (non-numeric after first 2 characters)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActorId("nmabcdef", queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidActorIDMessage);
      });
    });

    describe("validateActors", () => {
      it("should validate with no parameters", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActors({}, queryPrefix, querySuffix);
        assert.isTrue(validated);
        assert.isUndefined(errorResponse);
      });

      it("should validate with null", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActors( null, queryPrefix, querySuffix );
        assert.isTrue(validated);
        assert.isUndefined(errorResponse);
      });

      it("should validate with undefined", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActors( undefined, queryPrefix, querySuffix );
        assert.isTrue(validated);
        assert.isUndefined(errorResponse);
      });

      it("should validate with a valid q parameter", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActors({ q: "valid" }, queryPrefix, querySuffix);
        assert.isTrue(validated);
        assert.isUndefined(errorResponse);
      });

      it("should validate with a valid pageNumber parameter", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActors({ pageNumber: "100" }, queryPrefix, querySuffix);
        assert.isTrue(validated);
        assert.isUndefined(errorResponse);
      });

      it("should validate with a valid pageSize parameter", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActors({ pageSize: "200" }, queryPrefix, querySuffix);
        assert.isTrue(validated);
        assert.isUndefined(errorResponse);
      });

      it("should invalidate when q parameter is too long", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActors({ q: "this query is too long" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidQSearchMessage);
      });

      it("should invalidate when q parameter is too short", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActors({ q: "a" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidQSearchMessage);
      });

      it("should invalidate when pageNumber parameter cannot parse to an integer", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActors({ pageNumber: "number" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidPageNumberMessage);
      });

      it("should invalidate when pageNumber parameter is too large", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActors({ pageNumber: "20000" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidPageNumberMessage);
      });

      it("should invalidate when pageNumber parameter is too small", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActors({ pageNumber: "0" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidPageNumberMessage);
      });

      it("should invalidate when pageSize parameter cannot parse to an integer", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActors({ pageSize: "size" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidPageSizeMessage);
      });

      it("should invalidate when pageSize parameter is too large", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActors({ pageSize: "2000" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidPageSizeMessage);
      });

      it("should invalidate when pageSize parameter is too small", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateActors({ pageSize: "0" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidPageSizeMessage);
      });
    });

    describe("validateMovies", () => {
      it("should validate with no parameters", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({}, queryPrefix, querySuffix);
        assert.isTrue(validated);
        assert.isUndefined(errorResponse);
      });

      it("should validate with null", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies( null, queryPrefix, querySuffix );
        assert.isTrue(validated);
        assert.isUndefined(errorResponse);
      });

      it("should validate with undefined", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies( undefined, queryPrefix, querySuffix );
        assert.isTrue(validated);
        assert.isUndefined(errorResponse);
      });

      it("should validate with a valid genre", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ genre: "action" }, queryPrefix, querySuffix);
        assert.isTrue(validated);
        assert.isUndefined(errorResponse);
      });

      it("should validate with a valid year", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ year: "1999" }, queryPrefix, querySuffix);
        assert.isTrue(validated);
        assert.isUndefined(errorResponse);
      });

      it("should validate with a valid rating", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ rating: "9.3" }, queryPrefix, querySuffix);
        assert.isTrue(validated);
        assert.isUndefined(errorResponse);
      });

      it("should validate with a valid actorId", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ actorId: "nm123345" }, queryPrefix, querySuffix);
        assert.isTrue(validated);
        assert.isUndefined(errorResponse);
      });

      it("should invalidate with invalid q parameter", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ q: "a" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidQSearchMessage);
      });

      it("should invalidate with invalid pageNumber parameter", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ pageNumber: "0" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidPageNumberMessage);
      });

      it("should invalidate with invalid pageSize parameter", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ pageSize: "size" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidPageSizeMessage);
      });

      it("should invalidate with invalid genre parameter (null)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ genre: null }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidGenreMessage);
      });

      it("should invalidate with invalid genre parameter (undefined)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ genre: undefined }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidGenreMessage);
      });

      it("should invalidate with invalid genre parameter (too long)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ genre: "this is too long for a genre" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidGenreMessage);
      });

      it("should invalidate with invalid genre parameter (too short)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ genre: "ge" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidGenreMessage);
      });

      it("should invalidate with invalid year parameter (won't parse to an integer)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ year: "year" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidYearMessage);
      });

      it("should invalidate with invalid year parameter (too large)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ year: "3060" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidYearMessage);
      });

      it("should invalidate with invalid year parameter (too small)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ year: "1870" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidYearMessage);
      });

      it("should invalidate with invalid rating parameter (won't parse to an integer)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ rating: "rating" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidRatingMessage);
      });

      it("should invalidate with invalid rating parameter (too large)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ rating: "12.34" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidRatingMessage);
      });

      it("should invalidate with invalid rating parameter (too small)", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ rating: "-1" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidRatingMessage);
      });

      it("should invalidate with invalid actorId parameter", () => {
        const { validated, errorResponse } = APIValidationUtilities.validateMovies({ actorId: "actor" }, queryPrefix, querySuffix);
        assert.isFalse(validated);
        assert.equal(errorResponse.validationErrors[0].message, queryErrorMessages.invalidActorIDMessage);
      });
    });
  });

  let consoleController: ConsoleController;

  describe("CommandLineUtilities", () => {
    describe("parseArguments", () => {
      before(() => {
        // strip the spec 
        // when tests are executed, the spec (file path) goofs up command-line-args parsing them
        const specIndex = process.argv.findIndex(a => a.includes("test/e2e/**/*.ts"));
        process.argv.splice(specIndex, 1);

        consoleController = new ConsoleController(logService)
      });
      // save the command line arguments so they can be restored after each test
      const argvSave = JSON.parse(JSON.stringify(process.argv));
      const specIndex = argvSave.findIndex(a => a.includes("test/unit/**/*.ts"));
      if (specIndex >= 0) argvSave.splice(specIndex, 1);

      it("should pass with no validation errors", () => {
        process.argv = process.argv.concat(["--log-level", "info"]);
        assert(consoleController.parseArguments().validationMessages.length == 0);
      });

      it("should invalidate if keyvault-name is missing", () => {
        const { validationMessages } = consoleController.parseArguments();
        assert(validationMessages.length > 0);
      });

      it("should default --log-level to info", () => {
        const { values } = consoleController.parseArguments();
        assert(values["log-level"] && values["log-level"] == "info");
      });

      it("should set log-level to warn", () => {
        process.env.LOG_LEVEL = "warn";
        const { values } = consoleController.parseArguments();
        assert(values["log-level"] && values["log-level"] == "warn");
      });

      it("should invalidate if the value of log-level is not valid", () => {
        process.argv = process.argv.concat(["--log-level", "boffo"]);
        assert(consoleController.parseArguments().validationMessages.length > 0);
      });

      it("should invalidate if the value of log-level is not valid from env var", () => {
        process.env.LOG_LEVEL = "boffo";
        assert(consoleController.parseArguments().validationMessages.length > 0);
      });

      it("should show help when --help is provided", () => {
        process.argv = process.argv.concat(["--help"]);
        const { values } = consoleController.parseArguments();
        assert.exists(values["help"]);
      });

      afterEach(() => {
        process.argv = argvSave.slice();
        // Clear environment variables
        process.env.KEYVAULT_NAME = "";
        process.env.AUTH_TYPE = "";
        process.env.LOG_LEVEL = "";
      });
    });
  });

  describe("httpStatusUtilities", () => {
    describe("getHttpStatusCode", () => {
      it("should return the error code given", () => {
        const error = { code: HttpStatus.BAD_GATEWAY }
        const status = getHttpStatusCode(error)
        assert(status == HttpStatus.BAD_GATEWAY);
      });

      it("should return a 500 Internal Server error when it not a 404 or error is a string", () => {
        const error = "Houston we have a problem"
        const status = getHttpStatusCode(error)
        assert(status == HttpStatus.INTERNAL_SERVER_ERROR);
      });

      it("should return a 404 error", () => {
        const error = "This is a 404 error code"
        const status = getHttpStatusCode(error)
        assert(status == HttpStatus.NOT_FOUND);
      })
    });
  });
});

