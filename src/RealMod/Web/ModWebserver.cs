using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using CoiTelemetry.Abstractions;
using Mafi.Unity;
using UnityEngine;

namespace CoiTelemetry.RealMod.Web;

public class ModWebserver:IDisposable
{
    private readonly LiveDataHub _liveData;
    private readonly HttpListener _listener;
    private readonly Thread _thread;
    private readonly IModContext _context;
    private readonly AssetsDb _assetsDb;
    private volatile bool _running;

    public LiveDataHub LiveData => _liveData;
    
    public ModWebserver(IModContext context,AssetsDb assetsDb, int port=17891)
    {
        _context = context;
        _assetsDb = assetsDb;
        _liveData = new LiveDataHub();
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
            catch (HttpListenerException e)
            {
                _context.Logger.Info(e.Message);
                if (_running) throw;
            }
            catch (ObjectDisposedException e)
            {
                _context.Logger.Info(e.Message);
                return;
            }
            catch (Exception e)
            {
                _context.Logger.Info(e.Message);
                SafeLog(e);
            }
        }
    }
    private void HandleRequest(HttpListenerContext context)
    {
        _context.Logger.Info("Handling request");
        var request = context.Request;
        var response = context.Response;
        AddCorsHeaders(response);
        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 204;
            response.Close();
            return;
        }

        var path = request.Url?.AbsolutePath;
        if (path?.StartsWith("/Assets") == true)
        {
            Texture2D data = _assetsDb.GetSharedTexture(path.Substring(1));
            WriteBinary(response, data.EncodeToPNG());
            return;
        }
        switch (path)
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

    private static void WriteBinary(HttpListenerResponse response, byte[] bytes)
    {
        response.StatusCode = 200;
        response.ContentType="image/png";
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