serve:
	dotnet run --project src/dotNet101.Api

build:
	dotnet build

test:
	dotnet test

docker-test:
	docker compose run --rm test
