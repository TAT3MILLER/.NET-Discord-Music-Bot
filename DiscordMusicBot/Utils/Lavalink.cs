using System.Diagnostics;

namespace DiscordMusicBot.Utils;

public class Lavalink
{
    private static Process _process = new Process();

    public static void Start()
    {
        //Start Lavalink jar
        ProcessStartInfo psi = new ProcessStartInfo("java", " -jar " + Environment.CurrentDirectory + "/Lavalink.jar");
        psi.CreateNoWindow = true;
        psi.UseShellExecute = true;
        _process = new Process();
        _process.StartInfo = psi;
        _process.Start();

    }

    public static void Stop()
    {
        _process.Kill();
        _process.Dispose();
    }
}