#!/bin/bash

usage(){
echo """
$0 -d 'some description' -i 4 -p 2 -u http://some.url:23232 [Optional Args/Flags]

Required args:
    -d | --description DESCRPTN     Annotation description
    -i | --dashboard-id ID          Dashboard ID
    -p | --panel-id ID              Panel ID
    -u | --url URL                  Grafana URL:Port. Prefix with http:// or https://
                                    Example: http://grafana.abcd.net:31222
Optional args:
    -t | --tags COMMA-SEP-TAGS      List of comma separated Annotation Tags
    -a | --auth [user:pass | -]     Basic Auth info. If '-' is specified then
                                    read from stdin in 'user:pass' format.
    -r | --time-range START,END     Annotation start and end time in EPOCH seconds
                                    separated by comma
Optional Flags:
    -h | --help                     Show the usage
"""
}
echo_exit(){
    ec=$1;shift;echo -e "\n$@" >&2
    exit $ec
}

[[ "$#" -lt 3 ]] && usage && exit 0
while (( "$#" )); do key="$1" val="$2"
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

        -a|--auth)
        auth=$val
        if [[ "$auth" == "-" ]]; then
            read -t 2 -s auth
        fi 
        [[ -z $auth ]] && echo_exit 255 "Provide Basic Auth with pipe or arg"
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

if  [[ -z ${desc} ]] || \
    [[ -z ${dash_id} ]] || \
    [[ -z ${panel_id} ]] || \
    [[ -z ${url} ]]; then
    echo_exit 1 "Provide the required args. For Help: $0 -h"
fi

header=(-H "Accept: application/json")
header+=(-H "Content-Type: application/json")
[[ ! -z ${auth} ]] && header+=(-H "Authorization: Basic $(printf $auth | base64 -w 0)")

request='"dashboardId":'$dash_id',"panelId":'$panel_id',"text":"'$desc'"'
[[ ! -z ${time_start} ]] && request+=', "time":'$((time_start*1000))
[[ ! -z ${time_end} ]] && request+=',"timeEnd":'$((time_end*1000))
[[ ! -z ${tags} ]] && request+=',"tags":["'${tags//,/\",\"}'"]'

curl -X POST --url "${url}/api/annotations" \
    "${header[@]}" \
    --data "{${request}}"
#http -v POST "${url}" Accept:application/json Content-type:application/json 