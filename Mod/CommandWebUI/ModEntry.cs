using System;
using System.Text;
using System.Threading;
using StardewModdingAPI;




namespace CommandWebUI
{





    public class ModEntry : Mod
    {
        private Server? server;

        public override void Entry(IModHelper helper)
        {
            this.Monitor.Log("Starting embedded HTTP+WS server...", LogLevel.Info);


            var reader = new WebSocketReader();
            Console.SetIn(reader);
            server = new Server(this.Monitor, port: 8080,reader: reader);
            
            var thread = new Thread(server.Start);
            thread.IsBackground = true;
            thread.Start();
            
            Console.SetOut(new WebSocketWriter(server));
            

            
        }


    }
}
