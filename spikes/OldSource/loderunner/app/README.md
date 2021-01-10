# LodeRunner

> End-to-End testing and load generation

## Usage

> Some paramaters require `--run-loop` or have different default values

- LodeRunner [options]

- Options:

  - -s, --server `URL(s)`
    - Server(s) to test
      - Required
      - valid: one or more URLs

  - -f, --files `File(s)`
    - List of files to test
      - Required
      - valid: one or more JSON files

  - --tag `string`
    - Tag for log and observability logging
      - default: null
      - valid: string length [1:100]

  - -l, --sleep `int`
    - Sleep (ms) between each request
      - default: 0
      - default: 1000 with --run-loop
      - valid: >= 0

  - -j, --strict-json
    - Use strict json when parsing
      - default: false
      - valid: bool

  - -u, --base-url `URL`
    - Base url for remote test files
      - default: null
      - valid: valid URL

  - -v, --verbose
    - Display verbose results
      - default: true
      - default: false with --run-loop
      - valid: bool

  - -r, --run-loop
    - Run test(s) in an infinite loop
      - default: false
      - valid: bool

  - --verbose-errors
    - Log verbose error messages
      - default: false
      - valid: bool

  - --random
    - Run requests randomly
      - requires: --run-loop
      - default: false
      - valid: bool

  - --duration `int`
    - Test duration (seconds)
      - default: null
      - valid: > 0

  - -t, --timeout `int`
    - Request timeout (seconds)
      - default: 30s
      - valid: > 0

  - --max-concurrent `int`
    - Max concurrent requests
      - default: 100
      - valid: > 0

  - --max-errors `int`
    - Max validation errors before LodeRunner exits with error status
      - default: 10
      - valid: > 0

  - --delay-start `int`
    - Delay test start (seconds)
      - default: 0
      - valid: >= 0

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
