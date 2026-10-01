# solar-microgrid

Smart Solar Microgrid Trading System, a university practice project. A web app for Backoffice staff and Grid Operators, backed by a C# Web API that holds all the business logic and talks to MongoDB.

## Folders

api is the C# Web API. web is the React web app.

## Needed

.NET 10 SDK, Node 22.22 or newer, MongoDB running locally.

## Running the api

cd api, then dotnet run. Listens on http://localhost:5080.

## Running the web app

cd web, npm install, then npm run dev. Runs on http://localhost:5173 and reads the api address from web/.env.

## How we work

Every member commits from their own account, small steps, short lower case commit messages saying what changed.

## Who did what

| Member | Name | Contribution |
|---|---|---|
| 1 | Sithum Madhuranga | Login and roles, prosumer management, pending activations, IIS hosting and deployment |
| 2 | Christine Lowe | Microgrid node management, map location picker |
| 3 | Sathush Nanayakkara | Reservation management, booking rules |
| 4 | Nimnath Nadushka | Home page, operator tools |

## Hosting on IIS

The API is live at https://api-solarmicrogrid.sithum.dev, hosted on a Windows Server 2022 Azure VM.

1. Install the Web Server (IIS) role (Server Manager, Add Roles and Features).
2. Install the ASP.NET Core Hosting Bundle (from the .NET downloads page), then run `iisreset`.
3. Create a MongoDB Atlas cluster and set its connection string in `api/appsettings.Production.json` (not committed, see `.gitignore`), along with `Jwt:Key` and the `Seed:*` admin account fields.
4. On the dev machine: `cd api`, `dotnet publish SolarMicrogrid.Api.csproj -c Release -o publish`.
5. Copy the `publish` folder onto the server, e.g. to `C:\inetpub\solar-microgrid-api`.
6. In IIS Manager, add an application pool with .NET CLR version "No Managed Code", then add a site pointing at that folder, bound to port 80, with the DNS host name used for the API.
7. In the site's `web.config`, set `ASPNETCORE_ENVIRONMENT` to `Production` inside an `<environmentVariables>` block under `<aspNetCore>`.
8. Open inbound ports 80 and 443 on the VM's network security group.
9. Point a DNS A record at the VM's public IP for the API's hostname.
10. Run `win-acme` (wacs.exe) on the server to issue and install a free Let's Encrypt certificate for that hostname, which also adds the HTTPS binding and sets up auto-renewal.

## Demo video

Link to be added.
