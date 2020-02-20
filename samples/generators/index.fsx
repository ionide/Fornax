#r "../../src/Fornax.Core/bin/Release/netstandard2.0/Fornax.Core.dll"
#if !FORNAX
#load "../loaders/postloader.fsx"
#load "../loaders/customloader.fsx"
#endif

open Html

let injectWebsocketCode (webpage:string) =
    let websocketScript =
        """
        <script type="text/javascript">
          var wsUri = "ws://localhost:8080/websocket";
      function init()
      {
        websocket = new WebSocket(wsUri);
        websocket.onclose = function(evt) { onClose(evt) };
      }
      function onClose(evt)
      {
        console.log('closing');
        websocket.close();
        document.location.reload();
      }
      window.addEventListener("load", init, false);
      </script>
        """
    let head = "<head>"
    let index = webpage.IndexOf head
    webpage.Insert ( (index + head.Length + 1),websocketScript)

let generate' (ctx : SiteContents) (_: string) =
    let posts = ctx.TryGetValues<Postloader.Post> () |> Option.defaultValue Seq.empty

    let psts =
        posts
        |> Seq.toList
        |> List.map (fun p -> span [] [!! p.link] )

    let siteModel = ctx.TryGetValue<Customloader.CustomConfig> ()
    let gv = siteModel |> Option.map (fun s -> s.SomeGlobalValue) |> Option.defaultValue "NO DEAFULT"

    html [] [
        div [] psts
    ]

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    let disableLiveRefresh = ctx.TryGetValue<Postloader.PostConfig> () |> Option.map (fun n -> n.disableLiveRefresh) |> Option.defaultValue false
    generate' ctx page
    |> HtmlElement.ToString
    |> fun n -> if disableLiveRefresh then n else injectWebsocketCode n