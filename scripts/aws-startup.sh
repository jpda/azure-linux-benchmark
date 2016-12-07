#!/bin/bash

download() {
    url="<path to bench.sh>"
    target="/home/ubuntu/aws-bench.sh"
    wget -O $target $url
    chmod +x $target
    $target "<local path for test data> <blob container> <blob sas> <queue uri> <queue sas>" 
}

download