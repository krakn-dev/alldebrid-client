# Stage 1 - Build the frontend
FROM node:22.16.0-alpine3.22 AS node-build-env
ARG TARGETPLATFORM
ENV TARGETPLATFORM=${TARGETPLATFORM:-linux/amd64}
ARG BUILDPLATFORM
ENV BUILDPLATFORM=${BUILDPLATFORM:-linux/amd64}

RUN mkdir /appclient
WORKDIR /appclient

RUN apk add --no-cache git python3 py3-pip make g++

COPY client ./client
COPY root ./root
RUN \
   cd client && \
   echo "**** Building Code  ****" && \
   npm ci && \
   npm run build -- --output-path=out

RUN ls -FCla /appclient/root

# Stage 2 - Build the backend
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS dotnet-build-env
ARG TARGETPLATFORM
ENV TARGETPLATFORM=${TARGETPLATFORM:-linux/amd64}
ARG BUILDPLATFORM
ENV BUILDPLATFORM=${BUILDPLATFORM:-linux/amd64}
ARG VERSION=1.0.0

RUN mkdir /appserver
WORKDIR /appserver

COPY server ./server
RUN \
   echo "**** Building Source Code for $TARGETPLATFORM on $BUILDPLATFORM ****" && \
   cd server && \
   dotnet restore --no-cache AdbClient.sln && \
   dotnet test && \
   dotnet publish --no-restore -c Release -p:Version="$VERSION" -p:AssemblyVersion="$VERSION" -o out

# Stage 3 - Supply the matching multi-architecture ASP.NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS dotnet-runtime

# Stage 4 - Build runtime image
FROM ghcr.io/linuxserver/baseimage-alpine:3.22
ARG TARGETPLATFORM
ENV TARGETPLATFORM=${TARGETPLATFORM:-linux/amd64}
ARG BUILDPLATFORM
ENV BUILDPLATFORM=${BUILDPLATFORM:-linux/amd64}

# set version label
ARG BUILD_DATE
ARG VERSION=1.0.0
LABEL build_version="Linuxserver.io extended version:- ${VERSION} Build-date:- ${BUILD_DATE}"
ENV XDG_CONFIG_HOME="/config/xdg"
ENV ALLDEBRIDCLIENT_BRANCH="main"

RUN \
   mkdir -p /data/downloads /data/db && \
   echo "**** Install pre-reqs ****" && \
   apk add --no-cache bash curl icu-libs krb5-libs libgcc libintl libssl3 libstdc++ zlib

COPY --from=dotnet-runtime /usr/share/dotnet /usr/share/dotnet

RUN \
   echo "**** Setting permissions ****" && \
   chown -R abc:abc /data && \
   rm -rf \
   /tmp/* \
   /var/cache/apk/* \
   /var/tmp/* || true

ENV PATH="$PATH:/usr/share/dotnet"

# Copy files for app
WORKDIR /app
COPY --from=dotnet-build-env /appserver/server/out .
COPY --from=node-build-env /appclient/client/out/browser ./wwwroot
COPY --from=node-build-env /appclient/root/ /

# ports and volumes
EXPOSE 6500

# Check Status
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 CMD curl -f http://localhost:6500/health || exit 1
