# Multi-stage Dockerfile consolidating build, test-runner, exporter and runtime stages.
# Usage:
#  - Build artifacts image: docker build --target builder -t portfolio-builder:ci .
#  - Run exporter to copy artifacts to host: docker build --target exporter -t portfolio-exporter:ci . && docker run --rm -v $(pwd)/artifacts:/artifacts portfolio-exporter:ci
#  - Run tests: docker build --target test-runner -t portfolio-test-runner:ci . && docker run --rm -v $(pwd):/workspace -v $(pwd)/TestResults:/artifacts/TestResults portfolio-test-runner:ci
#  - Build runtime image: docker build --target runtime -t portfolio:latest .

################################################################################
# Base SDK image for building and test-runner
################################################################################
#mcr.microsoft.com/dotnet/sdk
FROM tirsvad/tirsvadcli_debian13_nginx:latest AS builder
#FROM mcr.microsoft.com/dotnet/sdk:latest AS builder
SHELL ["/bin/bash", "-lc"]
#SHELL ["/bin/bash"]
WORKDIR /src
ARG CONFIGURATION=Release

# Copy source into the image
COPY ./ /src/

#RUN wget https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.101/dotnet-sdk-10.0.101-linux-x64.tar.gz \
    #&& mkdir -p /usr/share/dotnet \
    #&& tar -zxf dotnet-sdk-10.0.101-linux-x64.tar.gz -C /usr/share/dotnet
#

# Install wasm-tools workload
#RUN dotnet workload install wasm-tools --skip-manifest-update

# Restore and build the main web project
RUN dotnet restore ./src/Portfolio/Portfolio.csproj && \
    dotnet build ./src/Portfolio/Portfolio.csproj -c Debug --no-restore

# Also build any other projects under /src so their build outputs (bin/obj) are produced
#RUN set -eux; \
    #find ./src -name '*.csproj' -print0 | xargs -0 -n1 -I{} dotnet build "{}" -c $CONFIGURATION --no-restore || true
#
# Publish the web project into /artifacts
#RUN dotnet publish ./src/Portfolio/Portfolio.csproj -c $CONFIGURATION -o /artifacts --no-build  || true
#RUN dotnet publish ./src/Portfolio/Portfolio.csproj -c $CONFIGURATION -o /artifacts

################################################################################
# Test-runner stage: runtime container that executes tests when started
################################################################################
FROM tirsvad/tirsvadcli_debian13_nginx:latest AS test-runner
SHELL ["/bin/bash", "-lc"]
WORKDIR /workspace

# Ensure environment variables are available inside the test-runner container
#ENV ASPNETCORE_ENVIRONMENT=Development \
    #DOCKER_DOTNET_TEST=true
    #DB_PORTFOLIO_HOST=host.docker.internal

# Copy the repo so tests can run without relying on a mounted workspace (optional)
COPY ./ /workspace
#COPY --from=builder /src /workspace

#RUN dotnet tool install --global dotnet-coverage --version 18.1.0
#RUN dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.5.1

# Ensure entrypoint script exists to run tests
COPY docker/run_tests.sh /run_tests.sh
RUN sed -i 's/\r$//' /run_tests.sh && chmod +x /run_tests.sh || true

ENTRYPOINT ["/run_tests.sh"]

################################################################################
# Runtime stage: production runtime that serves the published app
################################################################################
#FROM tirsvad/tirsvadcli_debian13_nginx:latest AS runtime
#SHELL ["/bin/bash", "-lc"]
#WORKDIR /app
#
## Copy published artifacts from builder
#COPY --from=builder /artifacts/ ./
#
## Copy nginx config and entrypoint
#RUN rm /etc/nginx/{sites-available,sites-enabled}/default || true
#COPY ./docker/nginx/default.conf /etc/nginx/sites-available/default
#RUN ln -s /etc/nginx/sites-available/default /etc/nginx/sites-enabled/default
#
## TODO Replace with cert
#RUN dotnet dev-certs https --trust
#
#COPY docker/entrypoint.sh /entrypoint.sh
#RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh && ls -la /entrypoint.sh
#
#ENTRYPOINT ["/entrypoint.sh"]