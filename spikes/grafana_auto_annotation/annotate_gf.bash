#!/bin/bash

usage(){
echo """
$0 -d 'DESCRIPTION' REQRD_ARGS [OPTIONAL ARGS] [-]
'-' at the end means: script will read user auth from stdin 
$0 -d 'Specific desc' -i 4 --panel-id 9 -u https://some.url:23232 -t realtag,tag2 - <<< \"\$auth_bash_var\"
$0 -d 'Runnin load' --dashboard-id 2 -p 6 -u https://some.url:23232 -r 1616836775,1616836795

Required args:
    -d | --description DESCRPTN     Annotation description
    -i | --dashboard-id ID          Dashboard ID
    -p | --panel-id ID              Panel ID
    -u | --url URL                  Grafana URL:Port. Prefix must be https://
                                    Example: https://grafana.abcd.net:31222
Optional args:
    -t | --tags COMMA-SEP-TAGS      List of comma separated Annotation Tags
    -r | --time-range START,END     Annotation start and end time in EPOCH seconds
                                    separated by comma
Optional Flags:
    -h | --help                     Show the usage

Passing Auth info:
    It expects basic auth info in the form of user:pass
    1. Passing '-' at the end
        Script will read auth info from stdin
        e.g.
        echo \$AUTH_VAR | $0 -d desc -i 4 -p 4 -u grafana-url.com -
        $0 -d desc -i 4 -p 2 -u https://grafana-url.com - <<< $(echo \"\$AUTH_VAR\")
        $0 -d desc -i 4 -p 6 -u https://localhost:10000 - <<< \"\$AUTH_VAR\"
    2. Export environment variable named GRAFANA_USER_PASS and call this script without '-' at the end
        $0 -d desc -i 4 -p 4 -u https://some.grafana.url:32000        
"""
}
echo_exit(){
    ec=$1;shift;echo -e "\n$@" >&2
    exit $ec
}

[[ "$#" -lt 3 ]] && usage && exit 0
while (( "$#" )); do
    key="$1"
    [[ "$key" == '-' ]] && [[ $# == 1 ]]  && break
    [[ $# -gt 1 ]] && val="$2" || echo_exit 1 "Provide an argument value for '${key}'"
    case "$key" in
        -d|--description)
        desc=$val; shift 2
        ;;

        -i|--dashboard-id)
        dash_id=$val; shift 2
        ;;

        -u|--url)
        url=$val
        [[ ${url:${#url}-1:1} == '/' ]] && url=${url:0:-1}
        shift 2
        ;;

        -p|--panel-id)
        panel_id=$val; shift 2
        ;;

        -t|--tags)
        tags=$val;
        shift 2
        ;;

        -r|--time-range)
        IFS=',' read -r time_start time_end <<< "$val"
        shift 2
        ;;

        -h|--help)
        usage && exit 0
        ;;

        -*|--*=) # unsupported flags
        echo_exit 1 "Error: Unsupported flag $1"
        ;;
    esac
done

if [[ "${key}" == '-' ]];then
    read -t 1 -s auth
else
    # It will fail if `-o pipefail` is set
    read -t 1 -s auth <<< "$GRAFANA_USER_PASS"
fi

if  [[ -z ${desc} ]] || \
    [[ -z ${dash_id} ]] || \
    [[ -z ${panel_id} ]] || \
    [[ -z ${url} ]] || \
    [[ -z ${auth} ]]; then
    echo_exit 1 "Provide the required args. For Help: $0 -h"
fi

if ! grep -Eqo '^https://' <<< "$url" ;then
    echo_exit 1 'Only HTTPS is supported'
fi
header=(-H "Accept: application/json")
header+=(-H "Content-Type: application/json")
[[ ! -z ${auth} ]] && header+=(-H "Authorization: Basic $(printf $auth | base64 -w 0)")

request='"dashboardId":'$dash_id',"panelId":'$panel_id',"text":"'$desc'"'
[[ ! -z "${time_start}" ]] && request+=', "time":'$((time_start*1000))
[[ ! -z "${time_end}" ]] && request+=',"timeEnd":'$((time_end*1000))
[[ ! -z ${tags} ]] && request+=',"tags":["'${tags//,/\",\"}'"]'

# For dev purpose/self-signed cert: https://stackoverflow.com/questions/38571099/how-can-i-set-the-certificates-in-curl
# And use -k param
curl --max-time 5 -X POST --url "${url}/api/annotations" \
    "${header[@]}" \
    --data "{${request}}"
#http -v POST "${url}" Accept:application/json Content-type:application/json
