# solar-microgrid

Smart Solar Microgrid Trading System, our SE4040 assignment at SLIIT. This repo has the C# Web API and the React web app for Backoffice staff and grid operators. The API holds all the business logic and uses MongoDB. The Android app is in a separate repo.

Git repositories:

- Web API and web app: https://github.com/sithummadhuranga/solar-microgrid-web
- Android app: https://github.com/sithummadhuranga/solar-microgrid-mobile

The API is hosted at https://api-solarmicrogrid.sithum.dev and the web app at https://solarmicrogrid.sithum.dev.

## Folders

api is the C# Web API. web is the React web app.

## Needed

.NET 10 SDK, Node 22.22 or newer, and MongoDB running locally.

## Running the api

The secrets are not in git. From the api folder, set them with user secrets first. The JWT key needs at least 32 characters.

```
dotnet user-secrets set "Jwt:Key" "a-long-random-key-of-at-least-32-characters"
dotnet user-secrets set "Seed:BackofficeEmail" "admin@example.com"
dotnet user-secrets set "Seed:BackofficeName" "Admin"
dotnet user-secrets set "Seed:BackofficePassword" "a-password-of-8-or-more-characters"
```

Then run `dotnet run`. It listens on http://localhost:5080. The first time it starts, it creates the Backoffice user from the Seed settings and adds some sample nodes and slots.

## Running the web app

In the web folder run `npm install`, copy `.env.example` to `.env`, then run `npm run dev`. It runs on http://localhost:5173.

In `.env`, `VITE_API_URL` is the api address (http://localhost:5080 when running locally) and `VITE_GOOGLE_MAPS_API_KEY` is for the map on the node form.

Log in with the Backoffice account from the Seed settings. Grid operators are added on the Web users page, and new prosumers are activated on the Pending activations page.

## Who did what

| Member | Name | Contribution |
|---|---|---|
| 1 | Sithum Madhuranga | Login and roles. Web users, prosumers and pending activations. The web layout and api helper. IIS hosting. On Android: login, register and profile. |
| 2 | Christine Lowe | Nodes and slots in the api and on the web. Sample node data. The map screen on Android. |
| 3 | Sathush Nanayakkara | Reservations: create, change, cancel and approve, with the 7 day and 12 hour rules. The web reservations page. On Android: reserve, modify, cancel, summary and QR screens. |
| 4 | Nimnath Nadushka | Booking lists, history and dashboards. QR verify and complete. The web booking monitor and operator home. On Android: dashboard, history, search, SQLite and the operator scan screens. |

Everyone commits from their own account. Sathush commits as Sathufit and as G S R Nanayakkara.

## Hosting on IIS

The API is hosted on Windows IIS. We made a Windows Server 2022 Datacenter virtual machine in Azure (the Azure Edition, not Core) with ports 80, 443 and 3389 open, then added the Web Server (IIS) role and the .NET 10 Hosting Bundle. IIS only accepted the site config after we ran the Hosting Bundle installer again and restarted the server.

The data is in MongoDB Atlas, because a MongoDB on a developer's machine cannot be reached from the server. We published the API with `dotnet publish SolarMicrogrid.Api.csproj -c Release -o publish` and copied the output to `C:\inetpub\solar-microgrid-api`. The production settings are in an `appsettings.Production.json` in that folder: the Atlas connection string, `Jwt:Key`, the `Seed` settings and `Cors:WebOrigin`. That file is in `.gitignore`, so it is not in git.

In IIS Manager there is an application pool set to No Managed Code and a site on port 80 for the api host name. The Default Web Site is stopped because it also used port 80. `web.config` sets `ASPNETCORE_ENVIRONMENT` to `Production`. A DNS A record points the host name at the VM's public IP, and win-acme (`wacs.exe`) got a free Let's Encrypt certificate, which added the https binding and a renewal task.

A request to `/api/stations` with no token returns 401, so the API is reachable and protected.

## Web app on Vercel

The web app is on Vercel, built from the `web` folder of this repo. `VITE_API_URL` and `VITE_GOOGLE_MAPS_API_KEY` are set in Vercel as normal variables, not secret ones. `web/vercel.json` sends every path to `index.html`, so refreshing a page like `/admin` works. The domain has a CNAME record that points at Vercel.

## Demo video

[Watch the demo video](https://mysliit-my.sharepoint.com/:v:/g/personal/it23294066_my_sliit_lk/IQAljAkOOszgTb6KeODPJiayAUKaTudZiSvAkkNeI6I1FOU?nav=eyJyZWZlcnJhbEluZm8iOnsicmVmZXJyYWxBcHAiOiJTdHJlYW1XZWJBcHAiLCJyZWZlcnJhbFZpZXciOiJTaGFyZURpYWxvZy1MaW5rIiwicmVmZXJyYWxBcHBQbGF0Zm9ybSI6IldlYiIsInJlZmVycmFsTW9kZSI6InZpZXcifX0%3D&e=oiL80W)
