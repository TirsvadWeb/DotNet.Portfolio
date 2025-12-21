# Multi-stage Dockerfile consolidating build, test-runner, exporter and runtime stages.
# Usage:
#  - Build artifacts image: docker build --target builder -t portfolio-builder:ci .
#  - Run exporter to copy artifacts to host: docker build --target exporter -t portfolio-exporter:ci . && docker run --rm -v $(pwd)/artifacts:/artifacts portfolio-exporter:ci
#  - Run tests: docker build --target test-runner -t portfolio-test-runner:ci . && docker run --rm -v $(pwd):/workspace -v $(pwd)/TestResults:/artifacts/TestResults portfolio-test-runner:ci
#  - Build runtime image: docker build --target runtime -t portfolio:latest .

################################################################################
# Base SDK image for building and test-runner
################################################################################
FROM tirsvad/tirsvadcli_debian13_nginx:latest AS builder
SHELL ["/bin/bash", "-lc"]
WORKDIR /src
ARG CONFIGURATION=Release

# Copy source into the image
COPY ./ /src/

# Restore and build the main web project
RUN dotnet restore ./src/Portfolio/Portfolio/Portfolio.csproj \
    && dotnet build ./src/Portfolio/Portfolio/Portfolio.csproj -c $CONFIGURATION --no-restore

# Also build any other projects under /src so their build outputs (bin/obj) are produced
RUN set -eux; \
    find ./src -name '*.csproj' -print0 | xargs -0 -n1 -I{} dotnet build "{}" -c $CONFIGURATION --no-restore || true

# Publish the web project into /artifacts
RUN dotnet publish ./src/Portfolio/Portfolio/Portfolio.csproj -c $CONFIGURATION -o /artifacts --no-build

################################################################################
# Exporter stage: runtime container that copies /artifacts from image to mounted host volume
################################################################################
#FROM tirsvad/tirsvadcli_debian13_nginx:latest AS exporter
#SHELL ["/bin/bash", "-lc"]

# Copy the published artifacts from builder into this image
#COPY --from=builder /artifacts /artifacts
#COPY docker/copy_artifacts.sh /copy_artifacts.sh
##RUN sed -i 's/\r$//' /copy_artifacts.sh && chmod +x /copy_artifacts.sh
#RUN chmod +x /copy_artifacts.sh

#ENTRYPOINT ["/copy_artifacts.sh"]

################################################################################
# Test-runner stage: runtime container that executes tests when started
################################################################################
FROM tirsvad/tirsvadcli_debian13_nginx:latest AS test-runner
SHELL ["/bin/bash", "-lc"]
WORKDIR /workspace

# Copy the repo so tests can run without relying on a mounted workspace (optional)
COPY --from=builder /src /workspace

# Ensure entrypoint script exists to run tests
COPY docker/run_tests.sh /run_tests.sh
RUN sed -i 's/\r$//' /run_tests.sh && chmod +x /run_tests.sh || true

ENTRYPOINT ["/run_tests.sh"]

################################################################################
# Runtime stage: production runtime that serves the published app
################################################################################
FROM tirsvad/tirsvadcli_debian13_nginx:latest AS runtime
SHELL ["/bin/bash", "-lc"]
WORKDIR /app

# Copy published artifacts from builder
COPY --from=builder /artifacts/ ./

# Copy nginx config and entrypoint
RUN rm /etc/nginx/{sites-available,sites-enabled}/default
COPY docker/nginx/default.conf /etc/nginx/sites-available/default
RUN ln -s /etc/nginx/sites-available/default /etc/nginx/sites-enabled/default
COPY docker/entrypoint.sh /entrypoint.sh
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh && ls -la /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]