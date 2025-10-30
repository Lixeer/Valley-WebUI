
using System.Collections.Concurrent;
using System.Text;

namespace CommandWebUI;

public class WebSocketWriter : TextWriter
{
    private readonly Server server;
    private readonly TextWriter original;

    public WebSocketWriter(Server server)
    {
        this.server = server;
        this.original = Console.Out;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(string value)
    {
        original.Write(value); // 保留控制台输出
        server.Broadcast(value); // 推送到网页
    }
}

public class WebSocketReader : TextReader
{
    private readonly BlockingCollection<string> inputQueue = new();
    

    // 网页端消息到达时调用
    public void PushInput(string line)
    {
        inputQueue.Add(line);
    }

    public override string ReadLine()
    {
        // 阻塞等待直到有输入（就像真实控制台）
        return inputQueue.Take();
    }

    public override int Read()
    {
        string line = ReadLine();
        if (line == null)
            return -1;

        // 模拟逐字符读取
        byte[] bytes = Encoding.UTF8.GetBytes(line + Environment.NewLine);
        using var ms = new MemoryStream(bytes);
        return ms.ReadByte();
    }
}