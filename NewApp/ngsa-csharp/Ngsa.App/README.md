# Web API

## Usage

- ngsa [options]

- Options:
  - -s, --data-service `data-service-url`
    - Data Service URL
      - default: `http://localhost:4122`
      - valid: URL

  - -l, --log-level `enum`
    - Log Level
      - valid: Critical | Debug | Error | Information | None | Trace | Warning
      - default: Warning

  - -d, --dry-run
    - Validates and displays configuration
      - default: false
      - valid: bool

  - --version
    - Show version information
      - Example: 0.0.8-0110-1751
      - default: false
      - valid: bool

  - -?, -h, --help
    - Show help and usage information
      - default: false
      - valid: bool

  - Precedence
    - --version
    - --help
    - --dry-run
