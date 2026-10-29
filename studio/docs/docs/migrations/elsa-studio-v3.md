# Upgrade `elsa-studio-v3` to a UI-only host

`elsaworkflows/elsa-studio-v3` is the Studio-only Blazor Server host
(`Elsa.Studio.Host.Server`). It has no Elsa API. 3.9.x tags of the same image
name ran `Elsa.ModularServer.Web` and therefore included a server.

3.10.0 does not publish this image. The UI-only host lands in 3.11.

## What to do

1. Run the API on `elsaworkflows/elsa-server-v3` or
   `elsaworkflows/elsa-server-and-studio-v3`.
2. Point Studio at that API:

   ```bash
   docker run --network <api-network> \
     -e ASPNETCORE_ENVIRONMENT=Development \
     -e HTTP_PORTS=8080 \
     -e Backend__Url=http://elsa-server:8080/elsa/api \
     -p 14000:8080 \
     elsaworkflows/elsa-studio-v3:latest
   ```

`Backend__Url` binds the host `Backend:Url` setting. Use a URL the Studio
container can reach (the Compose service name and container port, not
`localhost` on the host-mapped port). `--network` joins the Docker network the
API container runs on, so `elsa-server` resolves.
