#!/bin/bash
# FAST Tools Deployment Script
# Deploy FAST Tools to various targets (local, docker, cloud)

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check prerequisites
check_prerequisites() {
    print_info "Checking prerequisites..."
    
    # Check for .NET SDK
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK not found. Please install .NET 8.0 or later."
        print_info "Download from: https://dotnet.microsoft.com/download/dotnet/8.0"
        exit 1
    fi
    
    local dotnet_version=$(dotnet --version)
    print_success ".NET SDK ${dotnet_version} found"
}

# Function to build all projects
build_all() {
    print_info "Building all projects..."
    
    # Build Core
    if [ -d "FastTools.Core" ]; then
        print_info "Building FastTools.Core..."
        dotnet build FastTools.Core/FastTools.Core.csproj -c Release
    fi
    
    # Build CLI
    if [ -d "FastTools.CLI" ]; then
        print_info "Building FastTools.CLI..."
        dotnet build FastTools.CLI/FastTools.CLI.csproj -c Release
    fi
    
    # Build Web
    if [ -d "FastTools.Web" ]; then
        print_info "Building FastTools.Web..."
        dotnet build FastTools.Web/FastTools.Web.csproj -c Release
    fi
    
    # Build original Tools
    if [ -d "Tools" ]; then
        print_info "Building Tools..."
        dotnet build Tools/Tools.csproj -c Release
    fi
    
    print_success "All projects built successfully!"
}

# Function to publish for local deployment
publish_local() {
    print_info "Publishing for local deployment..."
    
    local output_dir="${1:-./publish}"
    
    # Create output directory
    mkdir -p "$output_dir"
    
    # Publish CLI
    if [ -d "FastTools.CLI" ]; then
        print_info "Publishing CLI..."
        dotnet publish FastTools.CLI/FastTools.CLI.csproj \
            -c Release \
            -o "$output_dir/cli" \
            --self-contained false
    fi
    
    # Publish Web
    if [ -d "FastTools.Web" ]; then
        print_info "Publishing Web..."
        dotnet publish FastTools.Web/FastTools.Web.csproj \
            -c Release \
            -o "$output_dir/web" \
            --self-contained false
    fi
    
    print_success "Published to $output_dir"
    print_info "To run CLI: cd $output_dir/cli && dotnet FastTools.CLI.dll"
    print_info "To run Web: cd $output_dir/web && dotnet FastTools.Web.dll"
}

# Function to create standalone executables
publish_standalone() {
    print_info "Publishing standalone executables..."
    
    local output_dir="${1:-./publish-standalone}"
    local runtime="${2:-linux-x64}"
    
    print_info "Runtime: $runtime"
    
    # Create output directory
    mkdir -p "$output_dir"
    
    # Publish CLI as standalone
    if [ -d "FastTools.CLI" ]; then
        print_info "Publishing CLI standalone..."
        dotnet publish FastTools.CLI/FastTools.CLI.csproj \
            -c Release \
            -r "$runtime" \
            -o "$output_dir/cli" \
            --self-contained true \
            -p:PublishSingleFile=true \
            -p:PublishTrimmed=true
    fi
    
    # Publish Web as standalone
    if [ -d "FastTools.Web" ]; then
        print_info "Publishing Web standalone..."
        dotnet publish FastTools.Web/FastTools.Web.csproj \
            -c Release \
            -r "$runtime" \
            -o "$output_dir/web" \
            --self-contained true \
            -p:PublishSingleFile=true
    fi
    
    print_success "Standalone executables created in $output_dir"
}

# Function to create Docker image
deploy_docker() {
    print_info "Building Docker image..."
    
    # Check for Docker
    if ! command -v docker &> /dev/null; then
        print_error "Docker not found. Please install Docker."
        exit 1
    fi
    
    # Create Dockerfile if it doesn't exist
    if [ ! -f "Dockerfile" ]; then
        print_info "Creating Dockerfile..."
        create_dockerfile
    fi
    
    local image_name="${1:-fasttools}"
    local tag="${2:-latest}"
    
    docker build -t "${image_name}:${tag}" .
    
    print_success "Docker image ${image_name}:${tag} created successfully!"
    print_info "To run: docker run -p 5000:8080 ${image_name}:${tag}"
}

# Function to create Dockerfile
create_dockerfile() {
    cat > Dockerfile << 'EOF'
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY FastTools.Core/FastTools.Core.csproj FastTools.Core/
COPY FastTools.Web/FastTools.Web.csproj FastTools.Web/
RUN dotnet restore FastTools.Web/FastTools.Web.csproj

# Copy everything else and build
COPY . .
WORKDIR /src/FastTools.Web
RUN dotnet build FastTools.Web.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish FastTools.Web.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "FastTools.Web.dll"]
EOF
    
    print_success "Dockerfile created"
}

# Function to create docker-compose file
create_docker_compose() {
    print_info "Creating docker-compose.yml..."
    
    cat > docker-compose.yml << 'EOF'
version: '3.8'

services:
  fasttools-web:
    build: .
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: unless-stopped
    volumes:
      - ./data:/app/data
EOF
    
    print_success "docker-compose.yml created"
    print_info "To start: docker-compose up -d"
}

# Function to run tests
run_tests() {
    print_info "Running tests..."
    
    # Look for test projects
    if [ -d "FastTools.Tests" ]; then
        dotnet test FastTools.Tests/FastTools.Tests.csproj
    else
        print_warning "No test projects found"
    fi
}

# Main script
main() {
    echo "========================================="
    echo "  FAST Tools Deployment Script"
    echo "========================================="
    echo ""
    
    case "${1:-help}" in
        build)
            check_prerequisites
            build_all
            ;;
        publish)
            check_prerequisites
            publish_local "${2}"
            ;;
        standalone)
            check_prerequisites
            publish_standalone "${2}" "${3}"
            ;;
        docker)
            check_prerequisites
            deploy_docker "${2}" "${3}"
            ;;
        compose)
            create_docker_compose
            ;;
        test)
            check_prerequisites
            run_tests
            ;;
        all)
            check_prerequisites
            build_all
            run_tests
            publish_local "./publish"
            print_success "Deployment complete!"
            ;;
        help|*)
            echo "Usage: $0 [command] [options]"
            echo ""
            echo "Commands:"
            echo "  build                    Build all projects"
            echo "  publish [dir]            Publish to local directory (default: ./publish)"
            echo "  standalone [dir] [runtime] Create standalone executables"
            echo "                           Runtime: linux-x64, win-x64, osx-x64 (default: linux-x64)"
            echo "  docker [name] [tag]      Build Docker image (default: fasttools:latest)"
            echo "  compose                  Create docker-compose.yml"
            echo "  test                     Run tests"
            echo "  all                      Build, test, and publish"
            echo "  help                     Show this help"
            echo ""
            echo "Examples:"
            echo "  $0 build"
            echo "  $0 publish ./output"
            echo "  $0 standalone ./bin win-x64"
            echo "  $0 docker myapp v1.0"
            echo "  $0 all"
            ;;
    esac
}

# Run main function
main "$@"
