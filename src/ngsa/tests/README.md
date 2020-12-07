# Test Docs

- TODO - Add Details about how this works in ci-cd

- You can run a complete end-to-end test using webv
  - You must create your secrets first
  - You will need a second bash console.

```bash

# from src/tests (first console)
./runtests

# wait for Application started. Press Ctrl+c to shutdown.

# from TestFiles (second console)
webv -s localhost:4120 -f baseline.json

```

> Coverage results are available in `src/tests/TestResults`
