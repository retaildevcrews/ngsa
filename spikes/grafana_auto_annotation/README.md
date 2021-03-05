# Annotating Grafana with HTTP API

Run the `annotate_gf.bash` script to create Grafana annotation.

> This script POSTs to Grafana Annotation API using `curl` and uses Basic Auth. It's not secure. Should be used with HTTPS and preferably Bearer token instead of `user:pass`

```bash

./annotate_gf.bash
"""
Required args:
    -d | --description DESCRPTN     Annotation description
    -i | --dashboard-id ID          Dashboard ID
    -p | --panel-id ID              Panel ID
    -u | --url URL                  Grafana URL:Port. Prefix with http:// or https://. Example: http://grafana.abcd.net:31222
Optional args:
    -t | --tags COMMA-SEP-TAGS      List of comma separated Annotation Tags
    -a | --auth [user:pass | -]     Basic Auth info. If '-' is specified then read from stdin in 'user:pass' format.
    -r | --time-range START,END     Annotation start and end time in EPOCH seconds separated by comma
Optional Flags:
    -h | --help                     Show the usage
"""
```

## Example usage


```bash

# Create an annotation without auth info:
./annotate_gf.bash -d "Load test for 'The REASON'" -i 4 -p 2 -u http://localhost:32000

# Create an annotation with piped username and pass:
echo admin:notadmin | ./annotate_gf.bash -d "Test Piped auth" -i 4 -p 2 -u http://localhost:32000 --auth -

# Create an annotation with username and pass as args:
./annotate_gf.bash -d "Test Arg auth" -i 4 -p 2 -u http://localhost:32000 --auth usern@me:passw0rd

# Create an annotation with specific tag:
./annotate_gf.bash -d "Test auth with a Tag" -i 4 -p 2 -u http://localhost:32000 --tags range,script --auth admin:tag

# Create an annotation with specific tag and date:
time_start=$(date +%s) # Annotation Start time
# Do some work
sleep 60
time_end=$(date +%s) # Annotation End time
echo user:pass | ./annotate_gf.bash -d "Test Ranged annotation" -i 4 -p 2 -u http://localhost:32000 --tags load,script -r $time_start,$time_end

```
