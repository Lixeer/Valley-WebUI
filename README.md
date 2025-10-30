# Valley-WebUI
Some mods for game StardewValley order to control WebUI


### WebSocket API Path:
`CommandWebUI`
| 终结点| 类型 | 描述|
|:-:|:-:|:-:|
| `/`| `websocket` | 长连接获取控制台输出|
| `/index`| `get`/`web page` | webui 页面|


```csharp
//以下代码处理请求
private async Task HandleWebSocketAsync(HttpListenerContext ctx)
        {   

            if (ctx.Request.Url.AbsolutePath != "/")   //如果路由不是 / 就结束，即总结点为/ 
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                ctx.Response.Close();
                return;
            }
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

                    
                    string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                   

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
```