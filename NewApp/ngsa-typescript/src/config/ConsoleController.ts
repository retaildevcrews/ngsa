import "reflect-metadata";
import { LogService } from "../services";
import commandLineArgs = require("command-line-args");
import commandLineUsage = require("command-line-usage");
import { sections } from "./cli-config";
import { buildVersion } from "./constants";

export class ConsoleController {
    constructor(private logService: LogService) { }

    // capture cli arguments and fetch application configuration
    async run() {
        const { validationMessages, values } = this.parseArguments();

        // handle --help
        if (values.help) {
            this.showHelp();
            process.exit();
        }

        // handle invalid values
        else if (validationMessages.length > 0) {
            validationMessages.forEach(m => console.error(m));
            this.showHelp();
            process.exit(-1);
        }

        // set the log level based on input
        // defaults to info
        this.logService.setLogLevel(values["log-level"]);

        // dry run or return
        if (values["dry-run"]) {
            this.dryRun(values);
            process.exit();
        }
    }

    public parseArguments() {
        const options: OptionDefinition[] = sections.find(s => s.header == "Options").optionList;
        let args;

        // environment variables
        const env = { "log-level": process.env.LOG_LEVEL };

        // command line arguments
        try {
            args = commandLineArgs(options);
        } catch(e) {
            this.showHelp(`Error: ${e.name}`);
            process.exit(-1);
        }
        
        // compose the two
        const values = { ...env, ...args };

        // set default values if no cli args or env vars provided
        if (!("log-level" in args) && !values["log-level"]) values["log-level"] = "info";

        const validationMessages = [];

        // check required arguments
        options.filter(o => o.required && !values[o.name])
            .forEach(o => validationMessages.push(`Missing ${o.name} argument`));

        // check validation patterns
        options.filter(o => o.validationPattern && !o.validationPattern.test(values[o.name]))
            .forEach(o => {
                if (values[o.name] == "CLI") {
                    validationMessages.push(`Value: "${values[o.name]}" for ${o.name} argument is not valid in production. Add the --dev flag or use MI`);
                    return;
                }
                validationMessages.push(`Value: "${values[o.name]}" for ${o.name} argument is not valid`)
            });

        return { validationMessages: validationMessages, values: values };
    }

    showHelp(message?: string) {
        if (message) console.log(message);
        console.log(commandLineUsage(sections));
    }

    dryRun(values) {
        console.log(`
            Version                       ${buildVersion}
            Logging Level                 ${values["log-level"]}
        `);
    }
}

export interface OptionDefinition {
    name: string;
    alias?: string;
    type?: any;
    description?: string;
    validationPattern?: RegExp;
    required?: boolean;
}
