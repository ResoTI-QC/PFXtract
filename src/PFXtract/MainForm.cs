using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PFXtract.Core;

namespace PFXtract;

public sealed class MainForm : Form
{
    // Page officielle ouverte lorsque l’utilisateur clique sur le numéro de version.
    private const string ProductUrl = "https://resoti.app/logiciels/pfxtract/";

    // Palette et ressources graphiques communes à toute l’application.
    private static readonly Color Navy = Color.FromArgb(24, 37, 59);
    private static readonly Color Blue = Color.FromArgb(37, 99, 235);
    private static readonly Color PaleBlue = Color.FromArgb(239, 246, 255);
    private static readonly Color Slate = Color.FromArgb(71, 85, 105);
    private static readonly Color Border = Color.FromArgb(219, 226, 236);
    private static readonly Image BrandLogo = LoadEmbeddedImage("PFXtract.Assets.reso-ti-logo.png");
    private static readonly Image HeaderIcon = LoadEmbeddedImage("PFXtract.Assets.certificate-download-icon.png");
    private static readonly Icon WindowIcon = LoadEmbeddedIcon("PFXtract.Assets.certificate-download-icon.ico");
    private static readonly string AppVersion = GetAppVersion();

    // Contrôles principaux conservés comme champs afin de pouvoir les actualiser
    // lorsque l’utilisateur change de langue sans redémarrer l’application.
    private readonly TextBox _filePath = new();
    private readonly TextBox _password = new();
    private readonly Button _browseButton = new();
    private readonly Button _loadButton = new();
    private readonly ListView _certificateList = new();
    private readonly Panel _detailsPanel = new();
    private readonly Label _detailsTitle = new();
    private readonly TableLayoutPanel _detailsTable = new();
    private readonly Label _emptyState = new();
    private readonly CheckBox _cerFormat = new();
    private readonly CheckBox _pemFormat = new();
    private readonly CheckBox _chainFormat = new();
    private readonly CheckBox _privateKeys = new();
    private readonly Button _extractButton = new();
    private readonly Button _hostingButton = new();
    private readonly Button _helpButton = new();
    private readonly LinkLabel _versionLink = new();
    private readonly ComboBox _languageSelector = new();
    private readonly Label _status = new();
    private Label _subtitle = null!;
    private Label _fileLabel = null!;
    private Label _passwordLabel = null!;
    private Label _privacyHint = null!;
    private Label _listTitle = null!;
    private Label _detailsPlaceholder = null!;
    private Label _languageLabel = null!;
    private CertificateBundle? _bundle;

    // Le français est la valeur par défaut (index 0); l’anglais utilise l’index 1.
    private bool IsEnglish => _languageSelector.SelectedIndex == 1;

    private string T(string french, string english) => IsEnglish ? english : french;

    private CultureInfo UiCulture => CultureInfo.GetCultureInfo(IsEnglish ? "en-CA" : "fr-CA");

    public MainForm()
    {
        Text = "PFXtract";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 650);
        Size = new Size(1060, 760);
        BackColor = Color.FromArgb(247, 249, 252);
        Font = new Font("Segoe UI", 9.5f);
        Icon = WindowIcon;
        AllowDrop = true;

        BuildInterface();
        ApplyLanguage();
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        FormClosed += (_, _) => _bundle?.Dispose();
    }

    private void BuildInterface()
    {
        // La fenêtre est divisée en quatre zones fixes : en-tête, import,
        // contenu des certificats et barre d’actions inférieure.
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(28, 22, 28, 22)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 154));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildImportCard(), 0, 1);
        root.Controls.Add(BuildContent(), 0, 2);
        root.Controls.Add(BuildFooter(), 0, 3);
    }

    private Control BuildHeader()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var logo = new PictureBox
        {
            Dock = DockStyle.Right,
            Width = 240,
            Margin = Padding.Empty,
            Image = BrandLogo,
            SizeMode = PictureBoxSizeMode.Zoom,
            AccessibleName = "Logo RésoTI"
        };
        var icon = new PictureBox
        {
            Location = new Point(0, 4),
            Size = new Size(52, 52),
            Image = HeaderIcon,
            SizeMode = PictureBoxSizeMode.Zoom,
            AccessibleName = "Icône certificat et téléchargement"
        };
        var title = new Label
        {
            Text = "PFXtract",
            Font = new Font("Segoe UI Semibold", 18f),
            ForeColor = Navy,
            AutoSize = true,
            Location = new Point(68, 3)
        };
        _subtitle = new Label
        {
            ForeColor = Slate,
            AutoSize = true,
            Location = new Point(70, 38)
        };
        panel.Controls.AddRange([icon, title, _subtitle, logo]);
        return panel;
    }

    private void ShowHelp()
    {
        using var help = new Form
        {
            Text = T($"Aide — PFXtract v{AppVersion}", $"Help — PFXtract v{AppVersion}"),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(590, 520),
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9.5f)
        };

        var title = new Label
        {
            Text = T("Comment utiliser PFXtract", "How to use PFXtract"),
            Font = new Font("Segoe UI Semibold", 16f),
            ForeColor = Navy,
            AutoSize = true,
            Location = new Point(28, 24)
        };
        var version = new Label
        {
            Text = $"Version {AppVersion}",
            ForeColor = Slate,
            AutoSize = true,
            Location = new Point(31, 59)
        };
        var instructions = new Label
        {
            Text = T(
                "1. Sélectionnez votre fichier PFX ou P12.\n\n" +
                "2. Entrez son mot de passe, puis cliquez sur Analyser.\n\n" +
                "3. Cochez les certificats à utiliser.\n\n" +
                "4. Cliquez sur Copier CRT / KEY / CA pour remplir les trois champs de votre hébergeur, " +
                "ou sur Extraction complète pour enregistrer les fichiers.",
                "1. Select your PFX or P12 file.\n\n" +
                "2. Enter its password, then click Analyze.\n\n" +
                "3. Check the certificates you want to use.\n\n" +
                "4. Click Copy CRT / KEY / CA to fill in your hosting provider’s three fields, " +
                "or Full extraction to save the files."),
            ForeColor = Navy,
            AutoSize = false,
            Location = new Point(31, 102),
            Size = new Size(525, 190)
        };
        var warning = new Label
        {
            Text = T(
                "Important : la clé privée affichée dans le mode CRT / KEY / CA n’est pas chiffrée. " +
                "Ne la transmettez jamais à une autre personne et ne la conservez pas dans un emplacement non sécurisé.",
                "Important: the private key shown in CRT / KEY / CA mode is not encrypted. " +
                "Never share it with anyone or store it in an unsecured location."),
            ForeColor = Color.FromArgb(153, 27, 27),
            BackColor = Color.FromArgb(254, 242, 242),
            AutoSize = false,
            Location = new Point(28, 300),
            Size = new Size(532, 64),
            Padding = new Padding(12, 9, 12, 8)
        };
        var moreInfoPrefix = T("Pour plus d’informations : ", "For more information: ");
        var moreInfo = new LinkLabel
        {
            Text = moreInfoPrefix + ProductUrl,
            AutoSize = true,
            Location = new Point(31, 380),
            LinkColor = Blue,
            ActiveLinkColor = Navy,
            VisitedLinkColor = Blue,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Cursor = Cursors.Hand,
            AccessibleDescription = T(
                "Ouvrir la page officielle de PFXtract",
                "Open the official PFXtract product page")
        };
        // Seule l’adresse est soulignée et ouvre la page officielle.
        moreInfo.Links.Clear();
        moreInfo.Links.Add(moreInfoPrefix.Length, ProductUrl.Length, ProductUrl);
        moreInfo.LinkClicked += (_, _) => OpenProductPage();
        var close = new Button
        {
            Text = T("Fermer", "Close"),
            DialogResult = DialogResult.OK,
            Location = new Point(450, 422),
            Size = new Size(110, 34)
        };
        StylePrimaryButton(close);
        help.Controls.AddRange([title, version, instructions, warning, moreInfo, close]);
        help.AcceptButton = close;
        help.CancelButton = close;
        help.ShowDialog(this);
    }

    private Control BuildImportCard()
    {
        var card = CreateCard();
        card.Padding = new Padding(20, 16, 20, 14);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 62,
            ColumnCount = 4,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 33));

        _fileLabel = CreateFieldLabel(string.Empty);
        _passwordLabel = CreateFieldLabel(string.Empty);
        grid.Controls.Add(_fileLabel, 0, 0);
        grid.Controls.Add(_passwordLabel, 2, 0);

        var filePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        filePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        StyleTextBox(_filePath, string.Empty);
        _filePath.Dock = DockStyle.Fill;
        _filePath.Margin = new Padding(0, 3, 8, 5);
        _filePath.ReadOnly = true;
        _filePath.AllowDrop = true;
        _filePath.DragEnter += OnDragEnter;
        _filePath.DragDrop += OnDragDrop;
        _browseButton.Text = "Parcourir…";
        StyleSecondaryButton(_browseButton);
        _browseButton.Dock = DockStyle.None;
        _browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _browseButton.Margin = new Padding(0, 3, 0, 5);
        _browseButton.Click += (_, _) => BrowseForPfx();
        filePanel.Controls.Add(_filePath, 0, 0);
        filePanel.Controls.Add(_browseButton, 1, 0);

        StyleTextBox(_password, string.Empty);
        _password.Dock = DockStyle.Fill;
        _password.Margin = new Padding(0, 3, 0, 5);
        _password.UseSystemPasswordChar = true;
        _password.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) LoadCertificates(); };

        _loadButton.Text = "Analyser";
        StylePrimaryButton(_loadButton);
        _loadButton.Margin = new Padding(12, 3, 0, 5);
        _loadButton.Dock = DockStyle.None;
        _loadButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _loadButton.Click += (_, _) => LoadCertificates();

        grid.Controls.Add(filePanel, 0, 1);
        grid.Controls.Add(_password, 2, 1);
        grid.Controls.Add(_loadButton, 3, 1);
        grid.Layout += (_, _) => AlignImportControls();

        _privacyHint = new Label
        {
            ForeColor = Color.FromArgb(21, 128, 61),
            BackColor = Color.FromArgb(240, 253, 244),
            AutoSize = false,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(8, 5, 0, 0)
        };
        layout.Controls.Add(grid, 0, 0);
        layout.Controls.Add(_privacyHint, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildContent()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 560,
            SplitterWidth = 12,
            Padding = new Padding(0, 16, 0, 10),
            BackColor = Color.FromArgb(247, 249, 252)
        };

        var listCard = CreateCard();
        listCard.Padding = new Padding(16);
        _listTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Font = new Font("Segoe UI Semibold", 11f),
            ForeColor = Navy
        };
        ConfigureCertificateList();
        _emptyState.TextAlign = ContentAlignment.MiddleCenter;
        _emptyState.ForeColor = Slate;
        _emptyState.Font = new Font("Segoe UI", 11f);
        _emptyState.Dock = DockStyle.Fill;
        listCard.Controls.Add(_emptyState);
        listCard.Controls.Add(_certificateList);
        listCard.Controls.Add(_listTitle);

        ConfigureDetailsPanel();
        split.Panel1.Controls.Add(listCard);
        split.Panel2.Controls.Add(_detailsPanel);
        return split;
    }

    private void ConfigureCertificateList()
    {
        _certificateList.Dock = DockStyle.Fill;
        _certificateList.View = View.Details;
        _certificateList.CheckBoxes = true;
        _certificateList.FullRowSelect = true;
        _certificateList.HideSelection = false;
        _certificateList.BorderStyle = BorderStyle.None;
        _certificateList.BackColor = Color.White;
        _certificateList.ForeColor = Navy;
        _certificateList.Visible = false;
        _certificateList.Columns.Add("Nom", 205);
        _certificateList.Columns.Add("Type", 75);
        _certificateList.Columns.Add("Expiration", 105);
        _certificateList.Columns.Add("Clé", 90);
        _certificateList.SelectedIndexChanged += (_, _) => ShowSelectedDetails();
        _certificateList.ItemChecked += (_, _) => BeginInvoke(UpdateExtractButton);
    }

    private void ConfigureDetailsPanel()
    {
        _detailsPanel.Dock = DockStyle.Fill;
        _detailsPanel.BackColor = Color.White;
        _detailsPanel.Padding = new Padding(20);
        _detailsPanel.Paint += PaintCardBorder;

        _detailsTitle.Text = "Détails du certificat";
        _detailsTitle.Dock = DockStyle.Top;
        _detailsTitle.Height = 42;
        _detailsTitle.Font = new Font("Segoe UI Semibold", 11f);
        _detailsTitle.ForeColor = Navy;

        _detailsTable.Dock = DockStyle.Fill;
        _detailsTable.ColumnCount = 1;
        _detailsTable.AutoScroll = true;
        _detailsTable.Visible = false;

        _detailsPlaceholder = new Label
        {
            Name = "DetailsPlaceholder",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Slate,
            Dock = DockStyle.Fill
        };
        _detailsPanel.Controls.Add(_detailsPlaceholder);
        _detailsPanel.Controls.Add(_detailsTable);
        _detailsPanel.Controls.Add(_detailsTitle);
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        var panel = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        _status.Text = "Prêt";
        _status.ForeColor = Slate;
        _status.AutoSize = false;
        _status.Dock = DockStyle.Fill;
        _status.TextAlign = ContentAlignment.MiddleLeft;

        _cerFormat.Text = "CER (binaire)";
        _cerFormat.Checked = true;
        _cerFormat.AutoSize = true;
        _cerFormat.Location = new Point(0, 26);
        _cerFormat.CheckedChanged += (_, _) => UpdateExtractButton();
        _pemFormat.Text = "PEM (texte)";
        _pemFormat.Checked = true;
        _pemFormat.AutoSize = true;
        _pemFormat.Location = new Point(120, 26);
        _pemFormat.CheckedChanged += (_, _) => UpdateExtractButton();

        _chainFormat.Text = "Chaîne complète (PEM)";
        _chainFormat.Checked = true;
        _chainFormat.AutoSize = true;
        _chainFormat.Location = new Point(0, 43);
        _chainFormat.CheckedChanged += (_, _) => UpdateExtractButton();
        _privateKeys.Text = "Clés privées chiffrées (.key)";
        _privateKeys.Checked = true;
        _privateKeys.AutoSize = true;
        _privateKeys.Location = new Point(174, 43);
        _privateKeys.CheckedChanged += (_, _) => UpdateExtractButton();

        var formatPanel = new Panel { Width = 390, Dock = DockStyle.Right };
        formatPanel.Controls.AddRange([_cerFormat, _pemFormat, _chainFormat, _privateKeys]);
        _extractButton.Text = "Extraction complète";
        _extractButton.Width = 170;
        _extractButton.Dock = DockStyle.Right;
        _extractButton.Margin = new Padding(12);
        _extractButton.Enabled = false;
        StylePrimaryButton(_extractButton);
        _extractButton.Click += (_, _) => ExportSelected();

        _hostingButton.Text = "Copier CRT / KEY / CA";
        _hostingButton.Width = 188;
        _hostingButton.Dock = DockStyle.Right;
        _hostingButton.Enabled = false;
        StyleSecondaryButton(_hostingButton);
        _hostingButton.Click += (_, _) => ShowHostingBundle();

        panel.Controls.Add(_status);
        panel.Controls.Add(formatPanel);
        panel.Controls.Add(_extractButton);
        panel.Controls.Add(_hostingButton);

        var bottomBar = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(0, 3, 0, 0)
        };
        _helpButton.Dock = DockStyle.Fill;
        _helpButton.Margin = Padding.Empty;
        StyleSecondaryButton(_helpButton);
        _helpButton.Click += (_, _) => ShowHelp();

        _languageLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Slate,
            Margin = new Padding(8, 0, 5, 0)
        };
        _languageSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageSelector.Dock = DockStyle.Fill;
        _languageSelector.Margin = new Padding(0, 1, 0, 2);
        _languageSelector.Items.AddRange(["Français", "English"]);
        _languageSelector.SelectedIndex = 0;
        _languageSelector.SelectedIndexChanged += (_, _) => ApplyLanguage();

        var bottomLeft = new TableLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 300,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        bottomLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        bottomLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        bottomLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        bottomLeft.Controls.Add(_helpButton, 0, 0);
        bottomLeft.Controls.Add(_languageLabel, 1, 0);
        bottomLeft.Controls.Add(_languageSelector, 2, 0);

        _versionLink.Text = $"Version {AppVersion}";
        _versionLink.Dock = DockStyle.Right;
        _versionLink.Width = 120;
        _versionLink.TextAlign = ContentAlignment.MiddleRight;
        _versionLink.LinkColor = Blue;
        _versionLink.ActiveLinkColor = Navy;
        _versionLink.VisitedLinkColor = Blue;
        _versionLink.LinkBehavior = LinkBehavior.HoverUnderline;
        _versionLink.Font = new Font("Segoe UI Semibold", 8.5f);
        _versionLink.Cursor = Cursors.Hand;
        _versionLink.LinkClicked += (_, _) => OpenProductPage();
        bottomBar.Controls.Add(bottomLeft);
        bottomBar.Controls.Add(_versionLink);

        footer.Controls.Add(panel, 0, 0);
        footer.Controls.Add(bottomBar, 0, 1);
        return footer;
    }

    private void ApplyLanguage()
    {
        // Tous les textes permanents sont réappliqués ici. Les messages ponctuels
        // utilisent T(...) directement au moment où ils sont affichés.
        _subtitle.Text = T(
            "Extrayez certificats et clés privées d’un fichier PFX ou P12, simplement et localement.",
            "Extract certificates and private keys from a PFX or P12 file, simply and locally.");
        _fileLabel.Text = T("Fichier PFX ou P12", "PFX or P12 file");
        _passwordLabel.Text = T("Mot de passe", "Password");
        _filePath.AccessibleDescription = T("Sélectionnez ou déposez un fichier ici", "Select or drop a file here");
        _password.AccessibleDescription = T("Mot de passe du PFX", "PFX password");
        _browseButton.Text = T("Parcourir…", "Browse…");
        _loadButton.Text = T("Analyser", "Analyze");
        _privacyHint.Text = T(
            "🔒  Traitement 100 % local — aucune donnée n’est transmise.",
            "🔒  100% local processing — no data is transmitted.");
        _listTitle.Text = T("Certificats trouvés", "Certificates found");
        _emptyState.Text = T(
            "Déposez un fichier PFX ici\nou utilisez le bouton Parcourir.",
            "Drop a PFX file here\nor use the Browse button.");
        _detailsTitle.Text = T("Détails du certificat", "Certificate details");
        _detailsPlaceholder.Text = T(
            "Sélectionnez un certificat\npour afficher ses détails.",
            "Select a certificate\nto view its details.");

        if (_certificateList.Columns.Count == 4)
        {
            _certificateList.Columns[0].Text = T("Nom", "Name");
            _certificateList.Columns[1].Text = T("Type", "Type");
            _certificateList.Columns[2].Text = T("Expiration", "Expiration");
            _certificateList.Columns[3].Text = T("Clé", "Key");
        }

        foreach (ListViewItem item in _certificateList.Items)
        {
            if (item.Tag is X509Certificate2 certificate && item.SubItems.Count > 1)
                item.SubItems[1].Text = IsCertificateAuthority(certificate) ? "CA" : T("Final", "End-entity");
        }

        _cerFormat.Text = T("CER (binaire)", "CER (binary)");
        _pemFormat.Text = T("PEM (texte)", "PEM (text)");
        _chainFormat.Text = T("Chaîne complète (PEM)", "Full chain (PEM)");
        _privateKeys.Text = T("Clés privées chiffrées (.key)", "Encrypted private keys (.key)");
        _extractButton.Text = T("Extraction complète", "Full extraction");
        _hostingButton.Text = T("Copier CRT / KEY / CA", "Copy CRT / KEY / CA");
        _helpButton.Text = T("Aide", "Help");
        _languageLabel.Text = T("Langue", "Language");
        _versionLink.AccessibleDescription = T(
            "Ouvrir la page officielle de PFXtract",
            "Open the official PFXtract product page");
        _status.Text = T("Prêt", "Ready");
        _status.ForeColor = Slate;
        ShowSelectedDetails();
    }

    private void OpenProductPage()
    {
        try
        {
            // UseShellExecute demande à Windows d’ouvrir l’URL dans le navigateur par défaut.
            Process.Start(new ProcessStartInfo(ProductUrl) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            ShowWarning(T(
                "Impossible d’ouvrir la page du produit dans le navigateur.",
                "Unable to open the product page in your browser."));
        }
    }

    private void BrowseForPfx()
    {
        using var dialog = new OpenFileDialog
        {
            Title = T("Choisir un fichier PFX ou P12", "Choose a PFX or P12 file"),
            Filter = T(
                "Certificats PKCS#12 (*.pfx;*.p12)|*.pfx;*.p12|Tous les fichiers (*.*)|*.*",
                "PKCS#12 certificates (*.pfx;*.p12)|*.pfx;*.p12|All files (*.*)|*.*"),
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _filePath.Text = dialog.FileName;
            _password.Focus();
            SetStatus(T(
                "Fichier sélectionné. Saisissez le mot de passe puis cliquez sur Analyser.",
                "File selected. Enter the password, then click Analyze."));
        }
    }

    private void LoadCertificates()
    {
        if (string.IsNullOrWhiteSpace(_filePath.Text))
        {
            ShowWarning(T("Choisissez d’abord un fichier PFX ou P12.", "Choose a PFX or P12 file first."));
            return;
        }

        SetBusy(true, T("Analyse du fichier en cours…", "Analyzing the file…"));
        try
        {
            var loaded = CertificateBundleReader.Load(_filePath.Text, _password.Text);
            _bundle?.Dispose();
            _bundle = loaded;
            PopulateCertificateList();
            SetStatus(T(
                $"{_bundle.Certificates.Count} certificat(s) trouvé(s). Cochez ceux à extraire.",
                $"{_bundle.Certificates.Count} certificate(s) found. Check the ones to extract."), false);
        }
        catch (CryptographicException)
        {
            ClearCertificates();
            ShowWarning(T(
                "Impossible d’ouvrir le PFX. Vérifiez le mot de passe et l’intégrité du fichier.",
                "Unable to open the PFX. Check the password and file integrity."));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            ClearCertificates();
            ShowWarning(IsEnglish
                ? "Unable to read the selected file. Check its path, format and access permissions."
                : ex.Message);
        }
        finally
        {
            _password.Clear();
            SetBusy(false);
        }
    }

    private void PopulateCertificateList()
    {
        _certificateList.BeginUpdate();
        _certificateList.Items.Clear();
        if (_bundle is not null)
        {
            foreach (var certificate in _bundle.Certificates)
            {
                var name = certificate.GetNameInfo(X509NameType.SimpleName, false);
                if (string.IsNullOrWhiteSpace(name)) name = T("Certificat sans nom", "Unnamed certificate");
                var key = certificate.GetRSAPublicKey()?.KeySize is int rsaSize
                    ? $"RSA {rsaSize}"
                    : certificate.GetECDsaPublicKey()?.KeySize is int ecSize ? $"EC {ecSize}" : "—";
                var item = new ListViewItem(name) { Tag = certificate, Checked = true };
                item.SubItems.Add(IsCertificateAuthority(certificate) ? "CA" : T("Final", "End-entity"));
                item.SubItems.Add(certificate.NotAfter.ToString("yyyy-MM-dd"));
                item.SubItems.Add(key);
                _certificateList.Items.Add(item);
            }
        }
        _certificateList.EndUpdate();
        _emptyState.Visible = _certificateList.Items.Count == 0;
        _certificateList.Visible = !_emptyState.Visible;
        if (_certificateList.Items.Count > 0)
        {
            _certificateList.Items[0].Selected = true;
            _certificateList.Select();
        }
        UpdateExtractButton();
    }

    private void ShowSelectedDetails()
    {
        if (_certificateList.SelectedItems.Count == 0 ||
            _certificateList.SelectedItems[0].Tag is not X509Certificate2 certificate)
            return;

        _detailsTable.SuspendLayout();
        _detailsTable.Controls.Clear();
        _detailsTable.RowStyles.Clear();
        AddDetail(T("Sujet", "Subject"), certificate.Subject);
        AddDetail(T("Émetteur", "Issuer"), certificate.Issuer);
        AddDetail(T("Valide du", "Valid from"), certificate.NotBefore.ToString("dd MMMM yyyy HH:mm", UiCulture));
        AddDetail(T("Valide jusqu’au", "Valid until"), certificate.NotAfter.ToString("dd MMMM yyyy HH:mm", UiCulture));
        AddDetail(T("Numéro de série", "Serial number"), certificate.SerialNumber);
        AddDetail(T("Empreinte SHA-1", "SHA-1 thumbprint"), GroupFingerprint(certificate.Thumbprint));
        AddDetail(T("Algorithme", "Algorithm"), certificate.SignatureAlgorithm.FriendlyName ?? certificate.SignatureAlgorithm.Value ?? "—");
        AddDetail(T("Clé privée présente dans le PFX", "Private key included in PFX"), certificate.HasPrivateKey
            ? T("Oui (elle ne sera pas exportée avec le certificat)", "Yes (it will not be exported with the certificate)")
            : T("Non", "No"));
        _detailsTable.Visible = true;
        if (_detailsPanel.Controls["DetailsPlaceholder"] is Control placeholder)
            placeholder.Visible = false;
        _detailsTable.ResumeLayout();
    }

    private void AddDetail(string label, string value)
    {
        var row = new Panel { Height = 59, Dock = DockStyle.Top, Padding = new Padding(0, 3, 0, 4) };
        var caption = new Label
        {
            Text = label.ToUpperInvariant(),
            Font = new Font("Segoe UI Semibold", 7.5f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 19
        };
        var content = new Label
        {
            Text = value,
            ForeColor = Navy,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Padding = new Padding(0, 2, 0, 0)
        };
        row.Controls.Add(content);
        row.Controls.Add(caption);
        _detailsTable.RowCount++;
        _detailsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 59));
        _detailsTable.Controls.Add(row, 0, _detailsTable.RowCount - 1);
    }

    private void ExportSelected()
    {
        // Les certificats cochés sont les seuls transmis au moteur d’export.
        if (_bundle is null) return;
        var selected = _certificateList.Items.Cast<ListViewItem>()
            .Where(item => item.Checked)
            .Select(item => (X509Certificate2)item.Tag!)
            .ToArray();
        if (selected.Length == 0)
        {
            ShowWarning(T("Cochez au moins un certificat à extraire.", "Check at least one certificate to extract."));
            return;
        }

        using var dialog = new FolderBrowserDialog
        {
            Description = T("Choisissez le dossier de destination", "Choose the destination folder"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var formats = (_cerFormat.Checked ? CertificateExportFormat.Cer : 0) |
                      (_pemFormat.Checked ? CertificateExportFormat.Pem : 0);
        string? privateKeyPassword = null;
        if (_privateKeys.Checked && selected.Any(c => c.HasPrivateKey))
        {
            using var passwordDialog = new ExportPasswordDialog(IsEnglish);
            if (passwordDialog.ShowDialog(this) != DialogResult.OK) return;
            privateKeyPassword = passwordDialog.ExportPassword;
        }
        try
        {
            var options = new CertificateExportOptions(
                formats,
                _privateKeys.Checked,
                privateKeyPassword,
                _chainFormat.Checked);
            var result = CertificateExporter.Export(selected, dialog.SelectedPath, options);
            SetStatus(T(
                $"Terminé : {result.CertificateCount} certificat(s), {result.PrivateKeyCount} clé(s) privée(s).",
                $"Done: {result.CertificateCount} certificate(s), {result.PrivateKeyCount} private key(s)."), false);
            var open = MessageBox.Show(this,
                T(
                    $"Extraction terminée.\n\n{result.CertificateCount} certificat(s)\n{result.PrivateKeyCount} clé(s) privée(s) chiffrée(s)\n{result.Files.Count} fichier(s) créé(s)\n\nDestination :\n{dialog.SelectedPath}\n\nOuvrir le dossier ?",
                    $"Extraction complete.\n\n{result.CertificateCount} certificate(s)\n{result.PrivateKeyCount} encrypted private key(s)\n{result.Files.Count} file(s) created\n\nDestination:\n{dialog.SelectedPath}\n\nOpen the folder?"),
                T("Extraction réussie", "Extraction successful"), MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (open == DialogResult.Yes)
                Process.Start(new ProcessStartInfo("explorer.exe", dialog.SelectedPath) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            ShowWarning(IsEnglish
                ? "Extraction failed. Check the destination permissions and export options."
                : "L’extraction a échoué : " + ex.Message);
        }
    }

    private void ShowHostingBundle()
    {
        // Ce mode prépare trois blocs prêts à coller dans les formulaires
        // d’hébergement courants : CRT, KEY et CABUNDLE.
        var selected = _certificateList.Items.Cast<ListViewItem>()
            .Where(item => item.Checked)
            .Select(item => (X509Certificate2)item.Tag!)
            .ToArray();
        try
        {
            var bundle = HostingBundleBuilder.Build(selected);
            using var dialog = new HostingBundleForm(bundle, IsEnglish);
            dialog.ShowDialog(this);
            SetStatus(T(
                "Les blocs CRT, KEY et CABUNDLE ont été préparés.",
                "The CRT, KEY and CABUNDLE blocks are ready."), false);
        }
        catch (CryptographicException ex)
        {
            ShowWarning(IsEnglish ? "No supported private key was found in the selected certificates." : ex.Message);
        }
        catch (ArgumentException ex)
        {
            ShowWarning(IsEnglish ? "Select at least one certificate." : ex.Message);
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = TryGetDroppedPfx(e.Data, out _) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (TryGetDroppedPfx(e.Data, out var path))
        {
            _filePath.Text = path;
            _password.Focus();
            SetStatus(T(
                "Fichier déposé. Saisissez son mot de passe puis cliquez sur Analyser.",
                "File dropped. Enter its password, then click Analyze."));
        }
    }

    private static bool TryGetDroppedPfx(IDataObject? data, out string path)
    {
        path = string.Empty;
        if (data?.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } files) return false;
        var extension = Path.GetExtension(files[0]);
        if (!extension.Equals(".pfx", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".p12", StringComparison.OrdinalIgnoreCase)) return false;
        path = files[0];
        return true;
    }

    private void ClearCertificates()
    {
        _bundle?.Dispose();
        _bundle = null;
        _certificateList.Items.Clear();
        _certificateList.Visible = false;
        _emptyState.Visible = true;
        _detailsTable.Visible = false;
        if (_detailsPanel.Controls["DetailsPlaceholder"] is Control placeholder)
            placeholder.Visible = true;
        UpdateExtractButton();
    }

    private void UpdateExtractButton()
    {
        _extractButton.Enabled = _certificateList.Items.Cast<ListViewItem>().Any(i => i.Checked) &&
                                 (_cerFormat.Checked || _pemFormat.Checked || _chainFormat.Checked || _privateKeys.Checked) &&
                                 !_loadButton.Enabled.Equals(false);
        _hostingButton.Enabled = _certificateList.Items.Cast<ListViewItem>().Any(i => i.Checked &&
            i.Tag is X509Certificate2 certificate && certificate.HasPrivateKey) && _loadButton.Enabled;
    }

    private void SetBusy(bool busy, string? message = null)
    {
        _loadButton.Enabled = !busy;
        _browseButton.Enabled = !busy;
        UseWaitCursor = busy;
        if (message is not null) SetStatus(message);
        if (!busy) UpdateExtractButton();
    }

    private void SetStatus(string message, bool subdued = true)
    {
        _status.Text = message;
        _status.ForeColor = subdued ? Slate : Color.FromArgb(21, 128, 61);
    }

    private void ShowWarning(string message)
    {
        SetStatus(message);
        MessageBox.Show(this, message, "PFXtract", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private static string GroupFingerprint(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "—";
        return string.Join(" ", Enumerable.Range(0, (value.Length + 1) / 2)
            .Select(i => value.Substring(i * 2, Math.Min(2, value.Length - i * 2))));
    }

    private static string GetAppVersion()
    {
        var version = typeof(MainForm).Assembly.GetName().Version;
        return version is null ? "1.2.1" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static bool IsCertificateAuthority(X509Certificate2 certificate)
    {
        return certificate.Extensions.OfType<X509BasicConstraintsExtension>()
            .Any(extension => extension.CertificateAuthority);
    }

    private static Image LoadEmbeddedImage(string resourceName)
    {
        using var stream = typeof(MainForm).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"La ressource graphique « {resourceName} » est introuvable.");
        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    }

    private static Icon LoadEmbeddedIcon(string resourceName)
    {
        using var stream = typeof(MainForm).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"La ressource d’icône « {resourceName} » est introuvable.");
        using var source = new Icon(stream);
        return (Icon)source.Clone();
    }

    private static Panel CreateCard()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        panel.Paint += PaintCardBorder;
        return panel;
    }

    private static void PaintCardBorder(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel) return;
        using var pen = new Pen(Border);
        e.Graphics.DrawRectangle(pen, 0, 0, panel.ClientSize.Width - 1, panel.ClientSize.Height - 1);
    }

    private static Label CreateFieldLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        ForeColor = Navy,
        Font = new Font("Segoe UI Semibold", 9f),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static void StyleTextBox(TextBox box, string accessibleDescription)
    {
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Font = new Font("Segoe UI", 10f);
        box.AccessibleDescription = accessibleDescription;
        box.Margin = Padding.Empty;
    }

    private void AlignImportControls()
    {
        var fieldHeight = Math.Max(_filePath.Height, _password.Height);
        if (_browseButton.Height != fieldHeight)
            _browseButton.Height = fieldHeight;
        if (_loadButton.Height != fieldHeight)
            _loadButton.Height = fieldHeight;
    }

    private static void StylePrimaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Blue;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI Semibold", 9.5f);
        button.Cursor = Cursors.Hand;
    }

    private static void StyleSecondaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Border;
        button.BackColor = PaleBlue;
        button.ForeColor = Blue;
        button.Font = new Font("Segoe UI Semibold", 9f);
        button.Cursor = Cursors.Hand;
    }

    private sealed class ExportPasswordDialog : Form
    {
        // Fenêtre dédiée au mot de passe qui protège les exports PKCS#8.
        private readonly bool _english;
        private readonly TextBox _password = new();
        private readonly TextBox _confirmation = new();
        private readonly Label _error = new();

        public string ExportPassword => _password.Text;

        public ExportPasswordDialog(bool english)
        {
            _english = english;
            Text = T("Protéger les clés privées", "Protect private keys");
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(470, 270);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            var title = new Label
            {
                Text = T("Choisissez un mot de passe d’export", "Choose an export password"),
                Font = new Font("Segoe UI Semibold", 14f),
                ForeColor = Navy,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            var explanation = new Label
            {
                Text = T(
                    "Les clés privées seront enregistrées au format PKCS#8 chiffré AES-256.\nConservez ce mot de passe : il sera nécessaire pour les utiliser.",
                    "Private keys will be saved as AES-256 encrypted PKCS#8 files.\nKeep this password: you will need it to use them."),
                ForeColor = Slate,
                AutoSize = true,
                Location = new Point(26, 57)
            };
            var passwordLabel = CreateDialogLabel(T("Mot de passe", "Password"), 102);
            var confirmationLabel = CreateDialogLabel(T("Confirmation", "Confirmation"), 158);
            ConfigurePasswordBox(_password, 124);
            ConfigurePasswordBox(_confirmation, 180);
            _error.ForeColor = Color.FromArgb(185, 28, 28);
            _error.AutoSize = true;
            _error.Location = new Point(26, 218);

            var cancel = new Button { Text = T("Annuler", "Cancel"), DialogResult = DialogResult.Cancel, Location = new Point(260, 228), Size = new Size(86, 32) };
            StyleSecondaryButton(cancel);
            var confirm = new Button { Text = T("Continuer", "Continue"), Location = new Point(354, 228), Size = new Size(92, 32) };
            StylePrimaryButton(confirm);
            confirm.Click += (_, _) => ValidatePassword();

            Controls.AddRange([title, explanation, passwordLabel, _password, confirmationLabel, _confirmation, _error, cancel, confirm]);
            AcceptButton = confirm;
            CancelButton = cancel;
        }

        private static Label CreateDialogLabel(string text, int top) => new()
        {
            Text = text,
            ForeColor = Navy,
            AutoSize = true,
            Location = new Point(26, top)
        };

        private static void ConfigurePasswordBox(TextBox box, int top)
        {
            box.UseSystemPasswordChar = true;
            box.Location = new Point(155, top - 4);
            box.Size = new Size(280, 27);
        }

        private void ValidatePassword()
        {
            if (_password.Text.Length < 8)
            {
                _error.Text = T("Utilisez au moins 8 caractères.", "Use at least 8 characters.");
                return;
            }
            if (_password.Text != _confirmation.Text)
            {
                _error.Text = T("Les deux mots de passe ne correspondent pas.", "The passwords do not match.");
                return;
            }
            DialogResult = DialogResult.OK;
        }

        private string T(string french, string english) => _english ? english : french;
    }

    private sealed class HostingBundleForm : Form
    {
        // Affiche séparément les trois blocs PEM attendus par les hébergeurs.
        private readonly HostingBundle _bundle;
        private readonly bool _english;

        public HostingBundleForm(HostingBundle bundle, bool english)
        {
            _bundle = bundle;
            _english = english;
            Text = "CRT / KEY / CABUNDLE — " + bundle.CertificateName;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 680);
            Size = new Size(900, 800);
            BackColor = Color.FromArgb(247, 249, 252);
            Font = new Font("Segoe UI", 9.5f);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(22, 18, 22, 18)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 32));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            var header = new Panel { Dock = DockStyle.Fill };
            header.Controls.Add(new Label
            {
                Text = T("Prêt à copier dans votre hébergeur", "Ready to copy to your hosting provider"),
                Font = new Font("Segoe UI Semibold", 15f),
                ForeColor = Navy,
                AutoSize = true,
                Location = new Point(0, 0)
            });
            header.Controls.Add(new Label
            {
                Text = T(
                    $"Certificat : {bundle.CertificateName}  •  {bundle.AuthorityCount} certificat(s) CA",
                    $"Certificate: {bundle.CertificateName}  •  {bundle.AuthorityCount} CA certificate(s)"),
                ForeColor = Slate,
                AutoSize = true,
                Location = new Point(2, 35)
            });
            root.Controls.Add(header, 0, 0);
            root.Controls.Add(CreatePemField(T("Certificat (CRT)", "Certificate (CRT)"), bundle.CertificateCrt, false), 0, 1);
            root.Controls.Add(CreatePemField(T("Clé privée (KEY)", "Private Key (KEY)"), bundle.PrivateKey, true), 0, 2);
            root.Controls.Add(CreatePemField(T("Chaîne d’autorités (CABUNDLE)", "Certificate Authority Bundle (CABUNDLE)"), bundle.CertificateAuthorityBundle, false), 0, 3);

            var footer = new Panel { Dock = DockStyle.Fill };
            var warning = new Label
            {
                Text = T(
                    "⚠ La clé privée ci-dessus n’est pas chiffrée. Ne la partagez jamais ailleurs.",
                    "⚠ The private key above is not encrypted. Never share it with anyone."),
                ForeColor = Color.FromArgb(185, 28, 28),
                AutoSize = true,
                Location = new Point(0, 17)
            };
            var close = new Button { Text = T("Fermer", "Close"), Dock = DockStyle.Right, Width = 110, DialogResult = DialogResult.OK };
            StylePrimaryButton(close);
            footer.Controls.Add(warning);
            footer.Controls.Add(close);
            root.Controls.Add(footer, 0, 4);
            Controls.Add(root);
            AcceptButton = close;
        }

        private Panel CreatePemField(string title, string content, bool sensitive)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 0, 10) };
            var displayContent = FormatPemForDisplay(content);
            var label = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Navy,
                Dock = DockStyle.Top,
                Height = 28
            };
            var copy = new Button
            {
                Text = T("Copier", "Copy"),
                Dock = DockStyle.Right,
                Width = 90,
                AccessibleName = T("Copier ", "Copy ") + title
            };
            StyleSecondaryButton(copy);
            var text = new TextBox
            {
                Text = string.IsNullOrEmpty(content)
                    ? T("Aucun certificat CA trouvé dans le PFX.", "No CA certificate was found in the PFX.")
                    : displayContent,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false,
                TabStop = false,
                HideSelection = true,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9f),
                BackColor = sensitive ? Color.FromArgb(255, 247, 237) : Color.White
            };
            copy.Enabled = !string.IsNullOrEmpty(content);
            copy.Click += (_, _) =>
            {
                Clipboard.SetText(displayContent);
                copy.Text = T("Copié ✓", "Copied ✓");
            };
            var contentPanel = new Panel { Dock = DockStyle.Fill };
            contentPanel.Controls.Add(text);
            contentPanel.Controls.Add(copy);
            panel.Controls.Add(contentPanel);
            panel.Controls.Add(label);
            return panel;
        }

        private static string FormatPemForDisplay(string content)
        {
            if (string.IsNullOrEmpty(content)) return string.Empty;
            return content.ReplaceLineEndings(Environment.NewLine).TrimEnd() + Environment.NewLine;
        }

        private string T(string french, string english) => _english ? english : french;
    }
}
