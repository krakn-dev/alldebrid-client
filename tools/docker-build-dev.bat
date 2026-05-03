docker stop alldebridclientdev
docker rm alldebridclientdev
docker build --tag alldebridclientdev .
docker run --cap-add=NET_ADMIN -d -v C:/Temp/AdbClient/:/data/downloads -v C:/Temp/AdbClient/:/data/db --log-driver json-file --log-opt max-size=10m -p 6500:6500 --name alldebridclientdev alldebridclientdev
docker exec -it alldebridclientdev /bin/bash