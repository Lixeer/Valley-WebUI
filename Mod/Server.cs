using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StardewModdingAPI;

namespace CommandWebUI
{
    public class Server
    {
        private readonly HttpListener listener;
        private readonly IMonitor monitor;
        private readonly ConcurrentBag<WebSocket> sockets = new();
        private string page;
        private bool running;
        public int Port;

        private readonly WebSocketReader Reader;

        public Server(IMonitor monitor, WebSocketReader reader ,int port = 8080)
        {
            Reader = reader;
            Port = port;
            this.monitor = monitor;
            listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{Port}/");
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods","CommandWebUI","index.html");
            page = File.ReadAllText(path);
            if (page == null)
            {
                page = "<h1>CommandWebUI</h1>";
            }
        }

        public void Start()
        {
            running = true;
            listener.Start();
            monitor.Log($"Server listening on http://localhost:{this.Port}/", LogLevel.Info);

            while (running)
            {
                try
                {
                    var ctx = listener.GetContext();
                    if (ctx.Request.IsWebSocketRequest)
                        _ = HandleWebSocketAsync(ctx);
                    else
                        _ = HandleHttpAsync(ctx);
                }
                catch (Exception ex)
                {
                    monitor.Log($"{ex}", LogLevel.Warn);
                }
            }
        }

        public void Stop()
        {
            running = false;
            listener.Stop();
            monitor.Log("Server stopped.", LogLevel.Info);
        }

        private async Task HandleHttpAsync(HttpListenerContext ctx)
        {
            //判断终结点
            monitor.Log($"{ctx.Request.Url.AbsolutePath}", LogLevel.Info);
            if (ctx.Request.Url.AbsolutePath != "/index")
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                ctx.Response.Close();
                return;
            }
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await using var writer = new StreamWriter(ctx.Response.OutputStream);



            await writer.WriteAsync($"{this.page}");



            await writer.FlushAsync();
            ctx.Response.Close();
        }
        

        private async Task HandleWebSocketAsync(HttpListenerContext ctx)
        {
            try
            {
                var wsCtx = await ctx.AcceptWebSocketAsync(null);
                var ws = wsCtx.WebSocket;
                sockets.Add(ws);
                monitor.Log("client connected", LogLevel.Info);

                var buffer = new byte[1024];
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    // 接收网页端消息
                    string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    //作为控制台输入

                    Console.WriteLine($"[WebSocket Input] {msg}");
                    Reader.PushInput(msg);
                }

                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                monitor.Log("client disconnected", LogLevel.Info);
                
            }
            catch (Exception ex)
            {
                monitor.Log($"{ex}", LogLevel.Warn);
            }
        }

        public async void Broadcast(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            foreach (var ws in sockets)
            {
                if (ws.State == WebSocketState.Open)
                    await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
