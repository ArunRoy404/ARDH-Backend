# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (better layer caching): copy only project/props files.
COPY Directory.Packages.props .
COPY src/CleanArchitecture/CleanArchitecture.csproj src/CleanArchitecture/
COPY src/CleanArchitecture.Shared/CleanArchitecture.Shared.csproj src/CleanArchitecture.Shared/
RUN dotnet restore src/CleanArchitecture/CleanArchitecture.csproj

# Now copy the rest of the source and publish.
COPY src/ src/
RUN dotnet publish src/CleanArchitecture/CleanArchitecture.csproj -c Release -o /app/publish --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "CleanArchitecture.dll"]
