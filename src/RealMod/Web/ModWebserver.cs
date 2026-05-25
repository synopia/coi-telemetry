using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using CoiTelemetry.Abstractions;

namespace CoiTelemetry.RealMod.Web;

public class ModWebserver:IDisposable
{
    private readonly LiveDataHub _liveData;
    private readonly HttpListener _listener;
    private readonly Thread _thread;
    private readonly IModContext _context;
    
    private volatile bool _running;

    public ModWebserver(IModContext context,LiveDataHub liveData, int port=17891)
    {
        _context = context;
        _liveData = liveData;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");

        _thread = new Thread(ServerLoop)
        {
            IsBackground = true,
            Name = "CoiTelemetry.Web.Server"
        };
    }

    public void Start()
    {
        _context.Logger.Info("Starting webserver on port 17891");
        _running = true;
        _listener.Start();
        _thread.Start();
    }

    private void ServerLoop()
    {
        while (_running)
        {
            try
            {
                var context = _listener.GetContext();
                HandleRequest(context);
            }
            catch (HttpListenerException)
            {
                if (_running) throw;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception e)
            {
                SafeLog(e);
            }
        }
    }
    private void HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        AddCorsHeaders(response);
        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 204;
            response.Close();
            return;
        }

        switch (request.Url?.AbsolutePath)
        {
            case "/api/health":
                WriteJson(response, "{\"ok\":true}");
                break;
            case "/api/latest":
                var latest = _liveData.GetLatest();
                response.Headers["X-CoI-Export-Version"] = latest.Version.ToString();
                WriteJson(response, latest.Json);
                break;
            default:
                response.StatusCode = 404;
                WriteJson(response, "{\"error\":\"not found\"}");
                break;
        }
    }

    private static void WriteJson(HttpListenerResponse response, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        response.StatusCode = 200;
        response.ContentType="application/json";
        response.ContentLength64 = bytes.Length;
        
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.OutputStream.Close();
    }

    private static void AddCorsHeaders(HttpListenerResponse response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        response.Headers.Add("Cache-Control", "no-store");
    }

    private static void SafeLog(Exception e)
    {
        try
        {
            Directory.CreateDirectory("logs");
            File.AppendAllText("logs/webserver_errors.log", $"[{DateTime.UtcNow:O}] {e}");
        }
        catch
        {
            // ignore
        }
    }

    public void Dispose()
    {
        _running = false;
        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch
        {
            //ignore
        }
        _thread.Join(millisecondsTimeout:2000);
    }
}