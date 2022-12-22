#!/bin/bash
set -e

[ -z $1 ] && echo "Missing version" && exit 1

version=$1
project=src/Microsoft.Extensions.Caching.ServiceStackRedis
dotnet build -p:version=$version -c Release $project
dotnet pack $project -c Release -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg --include-source -p:Version=$version -p:PackageID=ServiceStackRedisCache -o ./artifacts
dotnet pack $project -c Release -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg --include-source -p:Version=$version -p:PackageID=Microsoft.Extensions.Caching.ServiceStackRedis -o ./artifacts