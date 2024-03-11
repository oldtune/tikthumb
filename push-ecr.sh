#! /bin/bash
aws ecr get-login-password --region ap-southeast-1 | docker login --username AWS --password-stdin 298086761063.dkr.ecr.ap-southeast-1.amazonaws.com
docker build -t tikthumb .
docker tag tikthumb:latest 298086761063.dkr.ecr.ap-southeast-1.amazonaws.com/tikthumb:latest
docker push 298086761063.dkr.ecr.ap-southeast-1.amazonaws.com/tikthumb:latest