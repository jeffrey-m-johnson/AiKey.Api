.PHONY: build run

build:
	dotnet build

run:
	dotnet run --project AiKey.Api.csproj
