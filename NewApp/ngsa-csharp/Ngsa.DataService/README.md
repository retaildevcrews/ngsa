# Data Service

## Usage

- DataService [options]

> --in-memory, --no-cache and --perf-cache are exclusive - only one is allowed

- Options:
  - --cache-duration `int`
    - Cache for duration (seconds)
      - default: 300
      - valid: > 0

  - --in-memory
    - Use in-memory database
      - default: false
      - valid: bool

  - --no-cache
    - Don't cache results
      - default: false
      - valid: bool

  - --perf-cache `int`
    - Cache only when req / sec exceeds value
      - valid: > 0

  - --secrets-volume `path`
    - Secrets Volume Path
      - default: secrets
      - valid: file path
  
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
