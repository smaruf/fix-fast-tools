#!/usr/bin/env python3
"""
FAST Tools Runner Script
Runs the FAST message decoder tool in various modes (CLI, Web, GUI)
"""

import argparse
import os
import subprocess
import sys
import platform

def get_dotnet_command():
    """Check if dotnet is available"""
    try:
        subprocess.run(['dotnet', '--version'], capture_output=True, check=True)
        return 'dotnet'
    except (subprocess.CalledProcessError, FileNotFoundError):
        print("Error: .NET SDK not found. Please install .NET 8.0 or later.")
        print("Download from: https://dotnet.microsoft.com/download/dotnet/8.0")
        sys.exit(1)

def build_project(project_path):
    """Build a .NET project"""
    print(f"Building {project_path}...")
    result = subprocess.run(['dotnet', 'build', project_path], capture_output=True, text=True)
    if result.returncode != 0:
        print(f"Build failed:\n{result.stderr}")
        sys.exit(1)
    print("Build successful!")

def run_cli(args):
    """Run the CLI tool"""
    print("Starting FAST Tools CLI...")
    cli_project = os.path.join('FastTools.CLI', 'FastTools.CLI.csproj')
    
    if not os.path.exists(cli_project):
        print(f"Error: CLI project not found at {cli_project}")
        sys.exit(1)
    
    build_project(cli_project)
    
    # Pass additional arguments to the CLI
    cmd = ['dotnet', 'run', '--project', cli_project, '--']
    cmd.extend(args.cli_args if args.cli_args else [])
    
    subprocess.run(cmd)

def run_web(args):
    """Run the Web interface"""
    print("Starting FAST Tools Web Server...")
    web_project = os.path.join('FastTools.Web', 'FastTools.Web.csproj')
    
    if not os.path.exists(web_project):
        print(f"Error: Web project not found at {web_project}")
        sys.exit(1)
    
    build_project(web_project)
    
    port = args.port or 5000
    url = f"http://localhost:{port}"
    
    print(f"\n{'='*60}")
    print(f"  FAST Tools Web Interface")
    print(f"{'='*60}")
    print(f"  URL: {url}")
    print(f"  API Docs: {url}/openapi/v1.json")
    print(f"  Press Ctrl+C to stop")
    print(f"{'='*60}\n")
    
    env = os.environ.copy()
    env['ASPNETCORE_URLS'] = url
    
    try:
        subprocess.run(['dotnet', 'run', '--project', web_project], env=env)
    except KeyboardInterrupt:
        print("\nShutting down web server...")

def run_original_tools(args):
    """Run the original Tools console application"""
    print("Starting original FAST Tools...")
    tools_project = os.path.join('Tools', 'Tools.csproj')
    
    if not os.path.exists(tools_project):
        print(f"Error: Tools project not found at {tools_project}")
        sys.exit(1)
    
    build_project(tools_project)
    
    # Pass additional arguments
    cmd = ['dotnet', 'run', '--project', tools_project, '--']
    cmd.extend(args.tool_args if args.tool_args else [])
    
    subprocess.run(cmd)

def run_chinpak_tools(args):
    """Run the ChinPak Tools for DSE"""
    print("Starting ChinPak Tools for DSE...")
    chinpak_project = os.path.join('ChinPakTools.DSE', 'ChinPakTools.DSE.csproj')
    
    if not os.path.exists(chinpak_project):
        print(f"Error: ChinPakTools.DSE project not found at {chinpak_project}")
        sys.exit(1)
    
    build_project(chinpak_project)
    
    # Check if GUI mode is requested
    cmd = ['dotnet', 'run', '--project', chinpak_project]
    if args.gui:
        cmd.extend(['--', '--gui'])
    
    subprocess.run(cmd)

def show_info():
    """Show system and project information"""
    print("FAST Tools - System Information")
    print("=" * 60)
    print(f"Python Version: {sys.version}")
    print(f"Platform: {platform.platform()}")
    print(f"Architecture: {platform.machine()}")
    
    # Check .NET
    try:
        result = subprocess.run(['dotnet', '--version'], capture_output=True, text=True)
        print(f".NET SDK Version: {result.stdout.strip()}")
    except FileNotFoundError:
        print(".NET SDK: Not installed")
    
    print("\nAvailable Projects:")
    projects = [
        ('FastTools.Core', 'Core library for FAST message processing'),
        ('FastTools.CLI', 'Enhanced CLI tool with interactive mode'),
        ('FastTools.Web', 'Web API and UI for FAST message decoding'),
        ('Tools', 'Original console application'),
        ('ChinPakTools.DSE', 'Universal FIX/FAST/ITCH runner with CLI and GUI for DSE'),
    ]
    
    for name, desc in projects:
        exists = os.path.exists(name)
        status = "✓" if exists else "✗"
        print(f"  {status} {name}: {desc}")
    
    print("=" * 60)

def main():
    parser = argparse.ArgumentParser(
        description='FAST Tools Runner - Run FAST message decoder in various modes',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog='''
Examples:
  %(prog)s --cli                    # Run interactive CLI
  %(prog)s --cli --help             # Show CLI help
  %(prog)s --web                    # Start web server on port 5000
  %(prog)s --web --port 8080        # Start web server on custom port
  %(prog)s --tools                  # Run original console tool
  %(prog)s --chinpak                # Run ChinPak Tools (CLI mode)
  %(prog)s --chinpak --gui          # Run ChinPak Tools (GUI mode)
  %(prog)s --info                   # Show system information
        '''
    )
    
    parser.add_argument('--cli', action='store_true',
                        help='Run the enhanced CLI tool')
    parser.add_argument('--web', action='store_true',
                        help='Run the web interface')
    parser.add_argument('--tools', action='store_true',
                        help='Run the original console tool')
    parser.add_argument('--chinpak', action='store_true',
                        help='Run ChinPak Tools for DSE (FIX/FAST/ITCH universal runner)')
    parser.add_argument('--gui', action='store_true',
                        help='Launch GUI mode (use with --chinpak)')
    parser.add_argument('--port', type=int,
                        help='Port for web server (default: 5000)')
    parser.add_argument('--info', action='store_true',
                        help='Show system information')
    parser.add_argument('cli_args', nargs='*',
                        help='Arguments to pass to CLI tool')
    parser.add_argument('tool_args', nargs='*',
                        help='Arguments to pass to original tool')
    
    args = parser.parse_args()
    
    # Change to script directory
    script_dir = os.path.dirname(os.path.abspath(__file__))
    os.chdir(script_dir)
    
    # Check for .NET
    get_dotnet_command()
    
    if args.info:
        show_info()
    elif args.cli:
        run_cli(args)
    elif args.web:
        run_web(args)
    elif args.tools:
        run_original_tools(args)
    elif args.chinpak:
        run_chinpak_tools(args)
    else:
        # Default: show help and info
        parser.print_help()
        print()
        show_info()

if __name__ == '__main__':
    main()
