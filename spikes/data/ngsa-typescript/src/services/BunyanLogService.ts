import * as bunyan from "bunyan";
import { injectable } from "inversify";
import { v4 } from "uuid";
import { LogService } from "./LogService";

@injectable()
export class BunyanLogService implements LogService {
  private logger: bunyan;
  private uniqueServerId: string;
  private customId: string;

  // creates a new instance of the Bunyan Logger.

  /**
   * Log levels:
   * -----------
   * "fatal" (60):
   *   The service/app is going to stop or become unusable now. An operator should definitely look into this soon.
   * "error" (50):
   *   Fatal for a particular request, but the service/app continues servicing other requests.
   *   An operator should look at this soon(ish).
   * "warn" (40):
   *   A note on something that should probably be looked at by an operator eventually.
   * "info" (30):
   *   Detail on regular operation.
   * "debug" (20):
   *   Anything else, i.e. too verbose to be included in "info" level.
   * "trace" (10):
   *   Logging from external libraries used by your app or very detailed application logging.
   */
  constructor() {
    this.logger = bunyan.createLogger({
      name: "bunyanLog",
      serializers: {
        req: bunyan.stdSerializers.req,
        res: bunyan.stdSerializers.res,
      },
      stream: process.stdout,
      level: "info"
    });
    this.uniqueServerId = v4();
  }

  private logMessage (logLevel: string, message: string, id?: string) {
    const traceObj = { correlationID: this.uniqueServerId };

    if (id) {
        traceObj["customID"] = id;
    }
   
    this.logger[logLevel](traceObj, message);
  }

  public setLogLevel(logLevel) {
    logLevel = (!logLevel) ? "info" : logLevel;
    this.logger.level(logLevel);
  }

  public trace(message: string, id?: string) {
    this.logMessage("trace", message, id );
  }

  public info(message: string, id?: string) {
    this.logMessage("info", message, id );
  }

  public warn(message: string, id?: string) {
    this.logMessage("warn", message, id );
  }

  public error(error: Error, errorMessage: string, id?: string) {
    const traceObj = { err: error, correlationID: this.uniqueServerId };

    if (id) {
        traceObj["customID"] = id;
    }

    this.logger.error(traceObj, errorMessage);
  }
}
