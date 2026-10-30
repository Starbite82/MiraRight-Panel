using AioDisplay.App;
using System.Drawing;
using System.Windows.Forms;
internal static class AboutChecks
{
    [STAThread] public static void Main()
    {
        if (!AboutContent.Info.Contains("OpenAI Codex") || !AboutContent.Info.Contains(".NET 10")) throw new Exception("Development and framework information missing");
        string directory = Path.Combine(Directory.GetCurrentDirectory(), "about-fixture");
        Directory.CreateDirectory(directory);
        if (File.Exists(Path.Combine(directory, "IMPRINT.txt"))) File.Delete(Path.Combine(directory, "IMPRINT.txt"));
        if (AboutContent.ReadImprint(directory) != AboutContent.MissingImprint) throw new Exception("Missing imprint must remain visibly incomplete");
        const string fixture = "TESTDATEN – KEIN ECHTES IMPRESSUM\nAnbieter: Test\nKontakt: test@example.invalid";
        File.WriteAllText(Path.Combine(directory, "IMPRINT.txt"), fixture);
        if (AboutContent.ReadImprint(directory) != fixture) throw new Exception("Imprint UTF8 loading");
        using var form = new AboutWindow();
        form.Show(); Application.DoEvents();
        foreach (var button in form.Controls.OfType<Button>()) if (button.Text == "Impressum") button.PerformClick();
        using var image = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(image, new Rectangle(0, 0, image.Width, image.Height));
        image.Save("work/about-preview.png");
        form.Close();
        Console.WriteLine("PASS: Info/development, framework, missing imprint, UTF8, readable dialog");
    }
}
