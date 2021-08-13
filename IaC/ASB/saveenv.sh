#!/bin/bash

# change to the proper directory
cd $(dirname $0)

if [ -z "$ASB_DEPLOYMENT_NAME" ]
then
  echo "Please set ASB_DEPLOYMENT_NAME before running this script"
else
  if [ -f ${ASB_DEPLOYMENT_NAME}.asb.env ]
  then
    if [ "$#" = 0 ] || [ $1 != "-y" ]
    then
      read -p "asb.env already exists. Do you want to remove? (y/n) " response

      if ! [[ $response =~ [yY] ]]
      then
        echo "Please move or delete ${ASB_DEPLOYMENT_NAME}.asb.env and rerun the script."
        exit 1;
      fi
    fi
  fi

  echo '#!/bin/bash' > ${ASB_DEPLOYMENT_NAME}.asb.env
  echo '' >> ${ASB_DEPLOYMENT_NAME}.asb.env

  IFS=$'\n'

  for var in $(env | grep -E 'ASB_' | sort | sed "s/=/='/g")
  do
    echo "export ${var}'" >> ${ASB_DEPLOYMENT_NAME}.asb.env
  done

  cat ${ASB_DEPLOYMENT_NAME}.asb.env
fi
