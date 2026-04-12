using System;
using System.IO;
using System.Threading;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Transport;

public class LoginTool : IApplication
{
    private readonly ManualResetEventSlim _loggedOn = new(false);
    private SessionID? _sid;

    public void OnCreate(SessionID sessionID)
    {
        _sid = sessionID;
        Console.WriteLine($"[OnCreate] {sessionID}");
    }

    public void OnLogon(SessionID sessionID)
    {
        Console.WriteLine($"[OnLogon] {sessionID}");
        _loggedOn.Set();
    }

    public void OnLogout(SessionID sessionID)
    {
        Console.WriteLine($"[OnLogout] {sessionID}");
    }

    public void ToAdmin(Message message, SessionID sessionID)
    {
        // Inject Username/Password on LOGON (35=A)
        if (message.Header.GetString(Tags.MsgType) == MsgType.LOGON)
        {
            var s = Session.LookupSession(sessionID);
            if (s != null)
            {
                var settings = s.GetSessionSettings();
                if (settings.Has("Username"))
                    message.SetField(new Username(settings.GetString("Username")));
                if (settings.Has("Password"))
                    message.SetField(new Password(settings.GetString("Password")));
            }
        }

        Console.WriteLine($"[ToAdmin] {message}");
    }

    public void FromAdmin(Message message, SessionID sessionID)
    {
        Console.WriteLine($"[FromAdmin] {message}");
    }

    public void ToApp(Message message, SessionID sessionID)
    {
        Console.WriteLine($"[ToApp] {message}");
    }

    public void FromApp(Message message, SessionID sessionID)
    {
        Console.WriteLine($"[FromApp] {message}");
    }

    public static int Main(string[] args)
    {
        var cfgPath = args.Length > 0 ? args[0] : "fix.cfg";
        var timeoutSec = args.Length > 1 && int.TryParse(args[1], out var t) ? t : 10;

        if (!File.Exists(cfgPath))
        {
            Console.Error.WriteLine($"Config not found: {cfgPath}");
            return 2;
        }

        var settings = new SessionSettings(cfgPath);

        // Ensure dirs exist (based on [default])
        CreateDirIfPresent(settings, "FileStorePath");
        CreateDirIfPresent(settings, "FileLogPath");

        var app = new LoginTool();
        IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
        ILogFactory logFactory = new FileLogFactory(settings);
        IMessageFactory messageFactory = new DefaultMessageFactory();

        using var initiator = new SocketInitiator(app, storeFactory, settings, logFactory, messageFactory);

        Console.WriteLine($"Starting initiator: {cfgPath}");
        initiator.Start();

        Console.WriteLine($"Waiting for logon reply (timeout {timeoutSec}s)...");
        if (app._loggedOn.Wait(TimeSpan.FromSeconds(timeoutSec)))
        {
            Console.WriteLine("[RESULT] LOGON SUCCESS");
            initiator.Stop();
            return 0;
        }

        Console.WriteLine("[RESULT] LOGON TIMEOUT (no logon reply)");
        initiator.Stop();
        return 5;
    }

    private static void CreateDirIfPresent(SessionSettings settings, string key)
    {
        try
        {
            var d = settings.Get();
            if (d.Has(key))
            {
                var path = d.GetString(key);
                if (!string.IsNullOrWhiteSpace(path))
                    Directory.CreateDirectory(path);
            }
        }
        catch { }
    }
}