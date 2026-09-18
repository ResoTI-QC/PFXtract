using PFXtract;
using PFXtract.Core;
using System.Reflection;
using System.Security.Cryptography;

namespace PFXtract.VisualCheck;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using var form = new MainForm { Size = new Size(1060, 760) };
        form.Show();
        Application.DoEvents();
        foreach (var textBox in FindControls<TextBox>(form))
            textBox.Text = textBox.UseSystemPasswordChar
                ? "mot-de-passe"
                : @"C:\Certificats\exemple-serveur.pfx";
        Application.DoEvents();
        SaveForm(form, args[0]);
        if (args.Length > 2)
        {
            FindControls<ComboBox>(form).Single().SelectedIndex = 1;
            Application.DoEvents();
            SaveForm(form, args[2]);
        }
        form.Close();

        if (args.Length > 1)
            RenderHostingBundle(args[1], false);
        if (args.Length > 3)
            RenderHostingBundle(args[3], true);
    }

    private static void RenderHostingBundle(string outputPath, bool english)
    {
        var bundle = new HostingBundle(
            PemEncoding.WriteString("CERTIFICATE", RandomNumberGenerator.GetBytes(950)),
            PemEncoding.WriteString("PRIVATE KEY", RandomNumberGenerator.GetBytes(1_250)),
            PemEncoding.WriteString("CERTIFICATE", RandomNumberGenerator.GetBytes(1_600)),
            "exemple.domaine.ca",
            2);
        var formType = typeof(MainForm).GetNestedType("HostingBundleForm", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Fenêtre CRT / KEY / CABUNDLE introuvable.");
        using var dialog = (Form)(Activator.CreateInstance(formType, bundle, english)
            ?? throw new InvalidOperationException("Impossible de créer la fenêtre de vérification."));
        dialog.Show();
        Application.DoEvents();
        using var bitmap = new Bitmap(dialog.ClientSize.Width, dialog.ClientSize.Height);
        dialog.DrawToBitmap(bitmap, dialog.ClientRectangle);
        bitmap.Save(outputPath);
        dialog.Close();
    }

    private static void SaveForm(Form form, string outputPath)
    {
        using var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
        form.DrawToBitmap(bitmap, form.ClientRectangle);
        bitmap.Save(outputPath);
    }

    private static IEnumerable<T> FindControls<T>(Control parent) where T : Control
    {
        foreach (Control child in parent.Controls)
        {
            if (child is T match)
                yield return match;
            foreach (var descendant in FindControls<T>(child))
                yield return descendant;
        }
    }
}
