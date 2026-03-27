#!/bin/bash

## This script is here to replace the old scripts in Scripts/sh/ because they were pretty terrible

## Used to protect script runtime and prevent damages caused by invalid state.
set -euo pipefail

## Change dirs to the script directory because it is known, and we do not know where we are executing.
declare -r SH_ROOT="$(dirname "$(realpath "${BASH_SOURCE[0]}")")"
cd "$SH_ROOT" || {
        echo "Could not cd to \"${SH_ROOT}\", exiting"
        exit 1
}

## Gather variables from environment
UPDATE_SUBMODULES=${UPDATE_SUBMODULES:-true}		# if "false" do not update submodules, all other values skip updating submodules
RUN_MODE_BUILD=${RUN_MODE_BUILD:-false}			# if "false" do not build the project before running it

## For variables that cannot have default values, handle them here:
# . load_variable.sh
# load_variable EXIT_CODE EXAMPLE_VARIABLE_NAME "Could not load example variable. Exiting allscript."

## Define functions for different execution modes

build_mode() {
	submodule_update
	## Select the mode from the second argument
	if [ -v 1 ]; then
		mode="$1"
		shift
		case "$mode" in
			debug) # TODO default option
				dotnet build -c Debug $@
				return
				;;
			release)
				dotnet build -c Release $@
				return
				;;
			tools)
				dotnet build -c Tools $@
				return
				;;
		esac
	fi
	
	echo invalid target, please note it is case sensitive
	echo 'usage: ./allscript.sh build <debug|release|tools>'
	exit 1
}

run_mode() {
	no_build=""

	## Select the project from the second argument
        if [ -v 1 ]; then
                project="$1"
                shift
                case "$project" in
                        client)
				if [ "$RUN_MODE_BUILD" != "false" ]; then
					submodule_update
					no_build="--no-build"
				fi
                                dotnet run --project Content.Client $no_build $@
                                return
                                ;;
                        server)
				if [ "$RUN_MODE_BUILD" != "false" ]; then
					submodule_update
					no_build="--no-build"
				fi
                                dotnet run --project Content.Server $no_build $@
                                return
                                ;;
                        both) # TODO default option
				(run_mode server $@) &
				(run_mode client $@)
                                return
                                ;;
			build)
				RUN_MODE_BUILD=true
				run_mode $@
				return
				;;

                esac
        fi

        echo invalid project
        echo 'usage: ./allscript.sh run [build] <client|server|both>'
	exit 1
}

test_mode() {
	logdir="Scripts/logs/"
	mkdir -p Scripts/logs

	## Select the mode from the second argument
        if [ -v 1 ]; then
                mode="$1"
                shift
                case "$mode" in
                        tests) # TODO default option
				logf="${logdir}Content.IntegrationTests.log"
				if [ -e "$logf" ]; then
					rm "$logf"
				fi
				dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj -c DebugOpt -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed # > "$logf"
                                return
                                ;;
                        integration)
				logf="${logdir}Content.Tests.log"
                                if [ -e "$logf" ]; then
                                        rm "$logf"
                                fi
                                dotnet test Content.Tests/Content.Tests.csproj -c DebugOpt -- NUnit.ConsoleOut=0 > "$logf"
                                return
                                ;;
                        yaml)
				logf="${logdir}Content.YAMLLinter.log"
                                if [ -e "$logf" ]; then
                                        rm "$logf"
                                fi
				dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj -c DebugOpt -- NUnit.ConsoleOut=0 > "$logf"
                                return
                                ;;
                esac
        fi

        echo invalid target
        echo 'usage: ./allscript.sh test <tests|integration|yaml>'
        exit 1

	if [ -e "Scripts/logs" ]; then
		rm -r "${GODOT_HEPHAESTUS_ROOT}/gdext/hephaestus/${target_dir}/$bin_name"
	fi

	rm Scripts/logs/Content.IntegrationTests.log

}

clean_mode() {
	echo TODO
	echo \'dotnet clean\' should be simple enough in the meantime
	exit 0
}

submodule_update() {
	if [ "$UPDATE_SUBMODULES" != "false" ]; then
		echo Updating submodules!
		git submodule update --init --recursive
	fi
}

## Begin main lifecycle

## Select the mode from the first argument
if [ -v 1 ]; then
mode=$1
shift

case "$mode" in
	build)
		build_mode "$@"
		exit 0
		;;
	run)
		run_mode "$@"
		exit 0
		;;
	test)
		test_mode "$@"
		exit 0
		;;
	clean)
		clean_mode "$@"
		exit 0
		;;
esac
fi

echo invalid mode
echo 'usage: ./allscript.sh <build|run|test|clean>'
exit 1
