export const sections = [
    { "header": "Next Gen Symmetric Apps", "content": "A web app" },
    { "header": "Usage", "content": "npm start -- [options]" },
    {
        "header": "Options",
        "optionList": [
            {
                "name": "dry-run",
                "alias": "d",
                "type": Boolean,
                "description": "Validate configuration but does not run web server",
                "defaultValue": false
            },
            {
                "name": "log-level",
                "alias": "l",
                "type": String,
                "description": "Sets the log verboseness level, from highest to lowest: 'trace', 'info', 'warn', 'error', 'fatal'. Defaults to 'info'",
                "validationPattern": /^(trace|debug|info|warn|error|fatal)$/gi,
            },
            {
                "name": "help",
                "alias": "h",
                "type": Boolean,
                "description": "Print this usage guide.",
                "defaultValue": false
            }
        ]
    }
]
