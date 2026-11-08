using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static class Program
{
    internal static readonly string ToolDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YouTubeDownloader", "tools-2026.10.26-v1");

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--self-test")
        {
            try
            {
                PrepareTools();
                var report = new StringBuilder();
                foreach (string tool in new[] { "yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe", "deno.exe" })
                {
                    var info = new ProcessStartInfo(Path.Combine(ToolDir, tool), tool.StartsWith("ff") ? "-version" : "--version");
                    info.UseShellExecute = false;
                    info.CreateNoWindow = true;
                    info.RedirectStandardOutput = true;
                    using (var process = Process.Start(info))
                    {
                        string text = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        if (process.ExitCode != 0) throw new Exception(tool + " failed");
                        report.AppendLine(tool + ": " + text.Split('\n')[0].Trim());
                    }
                }
                File.WriteAllText(args[1], report.ToString(), Encoding.UTF8);
                return 0;
            }
            catch (Exception ex) { File.WriteAllText(args[1], ex.ToString()); return 1; }
        }
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DownloaderForm());
            return 0;
        }
        catch (Exception ex)
        {
            string path = Path.Combine(Path.GetTempPath(), "YouTubeDownloader-error.txt");
            File.WriteAllText(path, ex.ToString());
            MessageBox.Show("Не удалось открыть программу: " + ex.Message + "\nЖурнал: " + path);
            return 1;
        }
    }

    internal static void PrepareTools()
    {
        Directory.CreateDirectory(ToolDir);
        using (var mutex = new System.Threading.Mutex(false, "Local\\YouTubeDownloaderTools20261026v1"))
        {
            try { mutex.WaitOne(); } catch (System.Threading.AbandonedMutexException) { }
            try
            {
                if (File.Exists(Path.Combine(ToolDir, "ready")) &&
                    File.Exists(Path.Combine(ToolDir, "yt-dlp.exe")) &&
                    File.Exists(Path.Combine(ToolDir, "ffmpeg.exe")) &&
                    File.Exists(Path.Combine(ToolDir, "ffprobe.exe")) &&
                    File.Exists(Path.Combine(ToolDir, "deno.exe"))) return;
                using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("tools.zip"))
                using (var zip = new ZipArchive(resource, ZipArchiveMode.Read))
                {
                    foreach (var entry in zip.Entries)
                    {
                        string destination = Path.GetFullPath(Path.Combine(ToolDir, entry.FullName));
                        if (!destination.StartsWith(ToolDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                            throw new IOException("Invalid archive path");
                        Directory.CreateDirectory(Path.GetDirectoryName(destination));
                        entry.ExtractToFile(destination, true);
                    }
                }
                File.WriteAllText(Path.Combine(ToolDir, "ready"), "1");
            }
            finally { mutex.ReleaseMutex(); }
        }
    }

    internal static string Quote(string value)
    {
        var result = new StringBuilder("\"");
        int slashes = 0;
        foreach (char c in value)
        {
            if (c == '\\') { slashes++; continue; }
            if (c == '"') { result.Append('\\', slashes * 2 + 1); result.Append(c); }
            else { result.Append('\\', slashes); result.Append(c); }
            slashes = 0;
        }
        result.Append('\\', slashes * 2);
        return result.Append('"').ToString();
    }
}

internal sealed class DownloaderForm : Form
{
    private readonly TextBox url = new TextBox();
    private readonly TextBox folder = new TextBox();
    private readonly TextBox log = new TextBox();
    private readonly Button download = new Button();
    private readonly Button update = new Button();
    private readonly Button browse = new Button();
    private readonly Label status = new Label();
    private bool busy;

    internal DownloaderForm()
    {
        Text = "YouTube — скачать видео";
        ClientSize = new Size(790, 540);
        MinimumSize = new Size(680, 480);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 3, RowCount = 7 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        foreach (int height in new[] { 48, 30, 40, 40, 44, 30 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);
        var title = new Label { Text = "YouTube  ↓  Максимальное качество", AutoSize = true, Font = new Font("Segoe UI", 17, FontStyle.Bold) };
        layout.Controls.Add(title, 0, 0); layout.SetColumnSpan(title, 3);
        var hint = new Label { Text = "Лучшее доступное видео + аудио. Без перекодирования. Контейнер MKV.", AutoSize = true };
        layout.Controls.Add(hint, 0, 1); layout.SetColumnSpan(hint, 3);
        url.Dock = DockStyle.Fill;
        layout.Controls.Add(url, 0, 2); layout.SetColumnSpan(url, 2);
        download.Text = "Скачать"; download.Dock = DockStyle.Fill;
        layout.Controls.Add(download, 2, 2);
        folder.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "YouTube");
        folder.ReadOnly = true; folder.Dock = DockStyle.Fill;
        layout.Controls.Add(folder, 0, 3); layout.SetColumnSpan(folder, 2);
        browse.Text = "Выбрать папку"; browse.Dock = DockStyle.Fill;
        layout.Controls.Add(browse, 2, 3);
        var open = new Button { Text = "Открыть папку", Dock = DockStyle.Fill };
        layout.Controls.Add(open, 1, 4);
        update.Text = "Обновить yt-dlp"; update.Dock = DockStyle.Fill;
        layout.Controls.Add(update, 2, 4);
        status.Text = "Вставьте ссылку на видео YouTube."; status.AutoSize = true;
        layout.Controls.Add(status, 0, 5); layout.SetColumnSpan(status, 3);
        log.Multiline = true; log.ReadOnly = true; log.ScrollBars = ScrollBars.Both;
        log.WordWrap = false; log.Dock = DockStyle.Fill; log.Font = new Font("Consolas", 9);
        layout.Controls.Add(log, 0, 6); layout.SetColumnSpan(log, 3);
        AcceptButton = download;
        browse.Click += delegate {
            using (var dialog = new FolderBrowserDialog { SelectedPath = folder.Text })
                if (dialog.ShowDialog(this) == DialogResult.OK) folder.Text = dialog.SelectedPath;
        };
        open.Click += delegate {
            try { Directory.CreateDirectory(folder.Text); Process.Start("explorer.exe", Program.Quote(folder.Text)); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message); }
        };
        download.Click += async delegate { await Download(); };
        update.Click += async delegate { await RunOperation(new[] { "--ignore-config", "--update" }, "Обновление", "Обновление завершено."); };
        FormClosing += delegate(object sender, FormClosingEventArgs e) {
            if (busy) { e.Cancel = true; MessageBox.Show(this, "Дождитесь завершения текущей операции."); }
        };
    }

    private async Task Download()
    {
        Uri parsed;
        string input = url.Text.Trim();
        if (!Uri.TryCreate(input, UriKind.Absolute, out parsed) ||
            (parsed.Scheme != "https" && parsed.Scheme != "http") ||
            !(parsed.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase) ||
              parsed.Host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) ||
              parsed.Host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, "Вставьте полную ссылку на видео YouTube (https://...).");
            return;
        }
        await RunOperation(new[] {
            "--ignore-config", "--no-playlist", "--newline", "--windows-filenames",
            "--js-runtimes", "deno:" + Path.Combine(Program.ToolDir, "deno.exe"),
            "--format", "bestvideo*+bestaudio/best", "--merge-output-format", "mkv",
            "--ffmpeg-location", Program.ToolDir, "--paths", folder.Text,
            "--output", "%(title).180B [%(id)s].%(ext)s", "--", input
        }, "Загрузка", "Готово! Видео сохранено в выбранную папку.");
    }

    private async Task RunOperation(string[] arguments, string action, string success)
    {
        if (busy) return;
        busy = true; download.Enabled = update.Enabled = browse.Enabled = url.Enabled = false;
        log.Clear(); status.Text = "Подготовка компонентов — при первом запуске подождите…";
        try
        {
            await Task.Run((Action)Program.PrepareTools);
            Directory.CreateDirectory(folder.Text);
            status.Text = action + "…";
            int code = await Task.Run(() => RunProcess(arguments));
            status.Text = code == 0 ? success : "Ошибка. Подробности ниже. Попробуйте обновить yt-dlp.";
        }
        catch (Exception ex) { status.Text = "Ошибка: " + ex.Message; AppendLog(ex.ToString()); }
        finally { busy = false; download.Enabled = update.Enabled = browse.Enabled = url.Enabled = true; }
    }

    private int RunProcess(string[] arguments)
    {
        var command = new StringBuilder();
        foreach (string argument in arguments) command.Append(Program.Quote(argument)).Append(' ');
        var info = new ProcessStartInfo(Path.Combine(Program.ToolDir, "yt-dlp.exe"), command.ToString());
        info.UseShellExecute = false; info.CreateNoWindow = true;
        info.RedirectStandardOutput = info.RedirectStandardError = true;
        info.StandardOutputEncoding = info.StandardErrorEncoding = Encoding.UTF8;
        info.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
        using (var process = new Process { StartInfo = info })
        {
            process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) { if (e.Data != null) AppendLog(e.Data); };
            process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) { if (e.Data != null) AppendLog(e.Data); };
            process.Start(); process.BeginOutputReadLine(); process.BeginErrorReadLine();
            process.WaitForExit(); return process.ExitCode;
        }
    }

    private void AppendLog(string text)
    {
        if (InvokeRequired) { BeginInvoke(new Action<string>(AppendLog), text); return; }
        if (log.TextLength > 200000) log.Clear();
        log.AppendText(text + Environment.NewLine);
    }
}
