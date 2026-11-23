using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NexusPOO.Game.Data;
using NexusPOO.Game.Engine;
using NexusPOO.Game.Models;
using NexusTraining;
using System.Drawing.Drawing2D;
using System.Media;
using System.Text.Json;

namespace NexusPOO.Forms;

public sealed class NexusGameForm : Form
{
    private readonly Color bg = Color.FromArgb(7, 12, 25);
    private readonly Color panel = Color.FromArgb(13, 24, 43);
    private readonly Color cyan = Color.FromArgb(42, 220, 255);
    private readonly Color violet = Color.FromArgb(151, 92, 255);
    private readonly PartidaRepository repository = new();
    private readonly Panel root = new();
    private readonly Panel header = new();
    private readonly Panel leftHud = new();
    private readonly Panel rightHud = new();
    private readonly Panel footer = new();
    private readonly GameCanvas canvas;
    private readonly System.Windows.Forms.Timer gameTimer = new() { Interval = 33 };
    private readonly Label energyValue = new();
    private readonly Label stabilityValue = new();
    private readonly Label levelValue = new();
    private readonly Label inventoryValue = new();
    private readonly Label missionValue = new();
    private readonly Label messageValue = new();
    private readonly Label playerValue = new();
    private readonly Label zoneValue = new();
    private readonly Label shieldValue = new();
    private readonly Label xpValue = new();
    private readonly ProgressBar energyBar = new();
    private readonly ProgressBar stabilityBar = new();
    private readonly FlowLayoutPanel actionPanel = new();
    private readonly Panel overlay = new();
    private SoundPlayer? music;
    private SoundPlayer? sfx;
    private SoundPlayer? actionSound;
    private SoundPlayer? interactionSound;
    private GameWorld? world;
    private string profileName = "";
    private int currentSaveId;
    private string difficulty = "Normal";
    private bool inventoryOpen;
    private bool paused = true;
    private bool soundEnabled = true;
    private DateTime lastFrame = DateTime.UtcNow;
    private readonly HashSet<Keys> heldKeys = new();

    public NexusGameForm()
    {
        Text = "NEXUS // THE LOST ANOMALY";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1120, 720);
        BackColor = bg;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        KeyPreview = true;
        DoubleBuffered = true;

        canvas = new GameCanvas(() => world, () => inventoryOpen, () => paused);
        BuildLayout();
        BuildOverlay();
        LoadAudio();
        LoadLocalSettings();
        ShowMainMenu();

        gameTimer.Tick += (_, _) => UpdateGame();
        gameTimer.Start();
        KeyDown += OnGameKeyDown;
        KeyUp += (_, e) => heldKeys.Remove(e.KeyCode);
        FormClosing += (_, _) => { gameTimer.Stop(); music?.Stop(); };
    }

    private void BuildLayout()
    {
        root.Dock = DockStyle.Fill; root.BackColor = bg; Controls.Add(root);
        header.Dock = DockStyle.Top; header.Height = 72; header.BackColor = Color.FromArgb(5, 10, 22); header.Padding = new Padding(24, 10, 24, 10); root.Controls.Add(header);
        var logo = new Label { Text = "N E X U S", AutoSize = true, ForeColor = cyan, Font = new Font("Segoe UI", 24, FontStyle.Bold), Dock = DockStyle.Left, Width = 230, TextAlign = ContentAlignment.MiddleLeft };
        header.Controls.Add(logo);
        var tagline = new Label { Text = "EXPLORATION SYSTEM  //  IRIS SECTOR", AutoSize = true, ForeColor = Color.FromArgb(140, 173, 204), Font = new Font("Consolas", 10, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        header.Controls.Add(tagline);
        var status = new Label { Text = "● SYSTEM ONLINE", AutoSize = true, ForeColor = Color.FromArgb(90, 235, 180), Font = new Font("Consolas", 10, FontStyle.Bold), Dock = DockStyle.Right, TextAlign = ContentAlignment.MiddleRight, Width = 190 };
        header.Controls.Add(status);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = bg, Padding = new Padding(14, 12, 14, 10) };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        root.Controls.Add(body);
        leftHud.Dock = DockStyle.Fill; leftHud.BackColor = panel; leftHud.Padding = new Padding(16); leftHud.Margin = new Padding(0, 0, 10, 0); body.Controls.Add(leftHud, 0, 0);
        canvas.Dock = DockStyle.Fill; canvas.BackColor = Color.FromArgb(5, 11, 21); canvas.Margin = new Padding(0, 0, 10, 0); body.Controls.Add(canvas, 1, 0);
        rightHud.Dock = DockStyle.Fill; rightHud.BackColor = panel; rightHud.Padding = new Padding(15); rightHud.Margin = new Padding(0); body.Controls.Add(rightHud, 2, 0);
        BuildHud();
        footer.Dock = DockStyle.Bottom; footer.Height = 66; footer.BackColor = Color.FromArgb(5, 10, 22); footer.Padding = new Padding(18, 8, 18, 8); root.Controls.Add(footer);
        messageValue.Dock = DockStyle.Fill; messageValue.ForeColor = Color.FromArgb(155, 219, 237); messageValue.Font = new Font("Consolas", 10, FontStyle.Bold); messageValue.TextAlign = ContentAlignment.MiddleLeft; messageValue.Text = "SISTEMA LISTO // ESPERANDO OPERADOR"; footer.Controls.Add(messageValue);
        var pauseButton = MakeButton("Ⅱ  PAUSA", violet, 110, 38); pauseButton.Dock = DockStyle.Right; pauseButton.Click += (_, _) => ShowPauseMenu(); footer.Controls.Add(pauseButton);
    }

    private void BuildHud()
    {
        leftHud.Controls.Clear();
        var title = SectionLabel("OPERADOR"); title.Dock = DockStyle.Top; title.Height = 28; leftHud.Controls.Add(title);
        playerValue.Text = "SIN CADete"; playerValue.ForeColor = Color.White; playerValue.Font = new Font("Segoe UI", 13, FontStyle.Bold); playerValue.Dock = DockStyle.Top; playerValue.Height = 35; leftHud.Controls.Add(playerValue);
        levelValue.Text = "NIVEL 01  //  RANGO 0"; levelValue.ForeColor = cyan; levelValue.Font = new Font("Consolas", 9, FontStyle.Bold); levelValue.Dock = DockStyle.Top; levelValue.Height = 24; leftHud.Controls.Add(levelValue);
        AddBar(leftHud, "ENERGÍA", energyBar, energyValue);
        AddBar(leftHud, "ESTABILIDAD", stabilityBar, stabilityValue);
        var shield = SectionLabel("DEFENSA / EXPERIENCIA"); shield.Dock = DockStyle.Top; shield.Height = 32; leftHud.Controls.Add(shield);
        shieldValue.Text = "ESCUDO  0"; shieldValue.Dock = DockStyle.Top; shieldValue.Height = 24; shieldValue.ForeColor = Color.FromArgb(120, 220, 255); leftHud.Controls.Add(shieldValue);
        xpValue.Text = "XP  0 / 100"; xpValue.Dock = DockStyle.Top; xpValue.Height = 24; xpValue.ForeColor = Color.FromArgb(196, 164, 255); leftHud.Controls.Add(xpValue);
        var help = SectionLabel("CONTROLES"); help.Dock = DockStyle.Top; help.Height = 34; leftHud.Controls.Add(help);
        var keys = new Label { Text = "WASD / FLECHAS  Mover\nSHIFT  Correr\nESPACIO  Atacar\nE  Interactuar\nI  Inventario\n1–8  Usar objeto\nQ  Pulso de resonancia\nF1–F3  Cambiar sector\nF5  Guardar\nESC  Pausa", ForeColor = Color.FromArgb(151, 178, 203), Font = new Font("Consolas", 8.5f), Dock = DockStyle.Top, Height = 132 };
        leftHud.Controls.Add(keys);

        rightHud.Controls.Clear();
        var missionTitle = SectionLabel("OBJETIVO ACTUAL"); missionTitle.Dock = DockStyle.Top; missionTitle.Height = 28; rightHud.Controls.Add(missionTitle);
        missionValue.Dock = DockStyle.Top; missionValue.Height = 165; missionValue.ForeColor = Color.FromArgb(222, 236, 250); missionValue.Font = new Font("Segoe UI", 9.5f); missionValue.Text = "LA ANOMALÍA PERDIDA\n\nInicia una partida para recibir tu misión."; rightHud.Controls.Add(missionValue);
        var zoneTitle = SectionLabel("SECTOR ACTIVO"); zoneTitle.Dock = DockStyle.Top; zoneTitle.Height = 34; rightHud.Controls.Add(zoneTitle);
        zoneValue.Dock = DockStyle.Top; zoneValue.Height = 72; zoneValue.ForeColor = cyan; zoneValue.Font = new Font("Consolas", 10, FontStyle.Bold); rightHud.Controls.Add(zoneValue);
        var inventoryTitle = SectionLabel("INVENTARIO [I]"); inventoryTitle.Dock = DockStyle.Top; inventoryTitle.Height = 34; rightHud.Controls.Add(inventoryTitle);
        inventoryValue.Dock = DockStyle.Top; inventoryValue.Height = 150; inventoryValue.ForeColor = Color.FromArgb(177, 202, 226); inventoryValue.Font = new Font("Segoe UI", 9); rightHud.Controls.Add(inventoryValue);
        var actionsTitle = SectionLabel("ACCIONES RÁPIDAS"); actionsTitle.Dock = DockStyle.Top; actionsTitle.Height = 34; rightHud.Controls.Add(actionsTitle);
        actionPanel.Dock = DockStyle.Top; actionPanel.Height = 90; actionPanel.FlowDirection = FlowDirection.TopDown; actionPanel.WrapContents = false; actionPanel.BackColor = panel; rightHud.Controls.Add(actionPanel);
        AddQuickAction("ATACAR  [ESPACIO]", () => world?.Atacar());
        AddQuickAction("INTERACTUAR  [E]", () => world?.Interactuar());
        AddQuickAction("USAR CÉLULA  [1]", () => world?.UsarObjeto(0));
    }

    private static void AddBar(Control parent, string name, ProgressBar bar, Label value)
    {
        var label = new Label { Text = name, Dock = DockStyle.Top, Height = 22, ForeColor = Color.FromArgb(150, 182, 208), Font = new Font("Consolas", 8, FontStyle.Bold) }; parent.Controls.Add(label);
        bar.Dock = DockStyle.Top; bar.Height = 14; bar.Maximum = 100; bar.Style = ProgressBarStyle.Continuous; parent.Controls.Add(bar);
        value.Dock = DockStyle.Top; value.Height = 23; value.ForeColor = Color.White; value.Font = new Font("Consolas", 8); parent.Controls.Add(value);
    }
    private Label SectionLabel(string text) => new() { Text = text, ForeColor = violet, Font = new Font("Consolas", 9, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
    private void AddQuickAction(string text, Action action)
    {
        var b = MakeButton(text, Color.FromArgb(24, 52, 78), 210, 27); b.Height = 27; b.Font = new Font("Consolas", 8, FontStyle.Bold); b.Margin = new Padding(0, 2, 0, 2); b.Click += (_, _) => { action(); RefreshHud(); canvas.Invalidate(); }; actionPanel.Controls.Add(b);
    }

    private void BuildOverlay()
    {
        overlay.Dock = DockStyle.Fill; overlay.BackColor = Color.FromArgb(238, 4, 9, 22); overlay.Visible = false; overlay.Padding = new Padding(20);
        try { var imagePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Paisaje cósmico con planetas y montañas.png"); if (File.Exists(imagePath)) { overlay.BackgroundImage = Image.FromFile(imagePath); overlay.BackgroundImageLayout = ImageLayout.Stretch; } } catch { }
        root.Controls.Add(overlay); overlay.BringToFront();
    }

    private void LoadAudio()
    {
        try
        {
            string audioDir = Path.Combine(AppContext.BaseDirectory, "Sonidos");
            string musicPath = Path.Combine(audioDir, "MusicaJuego.wav");
            string sfxPath = Path.Combine(audioDir, "Inicio2.wav");
            string actionPath = Path.Combine(audioDir, "DespegueCohete.wav");
            string interactionPath = Path.Combine(audioDir, "Inicio.wav");
            if (File.Exists(musicPath)) music = new SoundPlayer(musicPath);
            if (File.Exists(sfxPath)) sfx = new SoundPlayer(sfxPath);
            if (File.Exists(actionPath)) actionSound = new SoundPlayer(actionPath);
            if (File.Exists(interactionPath)) interactionSound = new SoundPlayer(interactionPath);
        }
        catch { /* El juego sigue siendo jugable si el dispositivo de audio no está disponible. */ }
    }

    private void ShowMainMenu()
    {
        paused = true; heldKeys.Clear(); canvas.Invalidate(); overlay.Controls.Clear(); overlay.Visible = true;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent, Padding = new Padding(40) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45)); overlay.Controls.Add(layout);
        var hero = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var title = new Label { Text = "N E X U S", ForeColor = cyan, Font = new Font("Segoe UI", 52, FontStyle.Bold), Dock = DockStyle.Top, Height = 100, TextAlign = ContentAlignment.BottomLeft };
        var sub = new Label { Text = "THE LOST ANOMALY", ForeColor = Color.White, Font = new Font("Consolas", 20, FontStyle.Bold), Dock = DockStyle.Top, Height = 48 };
        var lore = new Label { Text = "UNA SEÑAL. TRES REALIDADES.\nUNA ANOMALÍA QUE NO DEBIÓ DESPERTAR.\n\nExplora los sectores IRIS, recupera el núcleo\ny regresa al punto de extracción.", ForeColor = Color.FromArgb(156, 188, 214), Font = new Font("Segoe UI", 12), Dock = DockStyle.Top, Height = 160 };
        hero.Controls.Add(lore); hero.Controls.Add(sub); hero.Controls.Add(title); layout.Controls.Add(hero, 0, 0);
        var menu = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(34, 24, 20, 10), BackColor = Color.FromArgb(210, 10, 22, 40) };
        layout.Controls.Add(menu, 1, 0);
        AddMenuButton(menu, "▶  NUEVA PARTIDA", () => ShowNewGameDialog(), true);
        AddMenuButton(menu, "▷  CONTINUAR PARTIDA", () => ContinueGame());
        AddMenuButton(menu, "▣  CARGAR PARTIDA", () => LoadGameDialog());
        AddMenuButton(menu, "◈  CONFIGURACIÓN", () => ShowSettings());
        AddMenuButton(menu, "⌁  ESTADÍSTICAS", () => ShowStatistics());
        AddMenuButton(menu, "★  LOGROS", () => ShowAchievements());
        AddMenuButton(menu, "ⓘ  CRÉDITOS", () => ShowCredits());
        AddMenuButton(menu, "✕  SALIR", () => Close());
        var hint = new Label { Text = "BUILD 2D // NEXUS TRAINING DIVISION", ForeColor = Color.FromArgb(90, 130, 164), Font = new Font("Consolas", 8), Dock = DockStyle.Bottom, Height = 26, TextAlign = ContentAlignment.MiddleCenter };
        menu.Controls.Add(hint);
    }

    private void ShowNewGameDialog()
    {
        var previousWorld = world;
        using var dialog = new Form { Text = "NUEVA EXPEDICIÓN", Size = new Size(460, 460), StartPosition = FormStartPosition.CenterParent, BackColor = panel, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 2, RowCount = 7 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62)); dialog.Controls.Add(grid);
        var profile = new TextBox { Text = profileName.Length == 0 ? "Jugador" : profileName, MaxLength = 100, Dock = DockStyle.Fill, BackColor = Color.FromArgb(7, 15, 29), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        var name = new TextBox { Text = "Explorador", MaxLength = 100, Dock = DockStyle.Fill, BackColor = Color.FromArgb(7, 15, 29), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        var age = new NumericUpDown { Minimum = 16, Maximum = 80, Value = 20, Dock = DockStyle.Fill, BackColor = Color.FromArgb(7, 15, 29), ForeColor = Color.White };
        var reality = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(7, 15, 29), ForeColor = Color.White }; reality.Items.AddRange(new object[] { "IRIS", "TERRA-PRIME", "NEXUS-ALFA" }); reality.SelectedIndex = 0;
        var diff = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(7, 15, 29), ForeColor = Color.White }; diff.Items.AddRange(new object[] { "Normal", "Difícil" }); diff.SelectedItem = difficulty == "Normal" ? "Normal" : difficulty;
        AddDialogRow(grid, 0, "PERFIL", profile); AddDialogRow(grid, 1, "NOMBRE DEL CADETE", name); AddDialogRow(grid, 2, "EDAD", age); AddDialogRow(grid, 3, "REALIDAD", reality); AddDialogRow(grid, 4, "DIFICULTAD", diff);
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        var start = MakeButton("INICIAR EXPEDICIÓN", cyan, 220, 42); start.ForeColor = bg; start.Click += (_, _) => { if (string.IsNullOrWhiteSpace(profile.Text) || string.IsNullOrWhiteSpace(name.Text)) { MessageBox.Show("Completa el perfil y el nombre del cadete."); return; } profileName = profile.Text.Trim(); difficulty = diff.SelectedItem?.ToString() == "Difícil" ? "Difícil" : "Normal"; world = new GameWorld(name.Text.Trim(), (int)age.Value, reality.SelectedItem?.ToString() ?? "IRIS", difficulty); currentSaveId = 0; inventoryOpen = false; paused = false; overlay.Visible = false; RefreshHud(); canvas.Focus(); PlaySound(); dialog.DialogResult = DialogResult.OK; dialog.Close(); };
        grid.Controls.Add(start, 0, 5); grid.SetColumnSpan(start, 2);
        var cancel = MakeButton("VOLVER", Color.FromArgb(35, 55, 80), 220, 36); cancel.Click += (_, _) => { dialog.DialogResult = DialogResult.Cancel; dialog.Close(); }; grid.Controls.Add(cancel, 0, 6); grid.SetColumnSpan(cancel, 2);
        dialog.ShowDialog(this);
        if (world != null && !ReferenceEquals(world, previousWorld))
        {
            paused = true;
            MessageBox.Show("TUTORIAL DE CAMPO\n\nWASD o flechas: moverte por el sector.\nESPACIO: atacar entidades cercanas.\nE: analizar anomalías, recoger el núcleo e interactuar con extracción.\nI: consultar inventario; teclas 1–4: usar objetos.\nF1–F3: cambiar entre sectores IRIS.\nF5: guardar la expedición en SQL Server.\n\nCruzar montañas, agua y bosques consume energía.", "PROTOCOLO DE ENTRENAMIENTO NEXUS", MessageBoxButtons.OK, MessageBoxIcon.Information);
            paused = false; canvas.Focus();
        }
    }

    private static void AddDialogRow(TableLayoutPanel grid, int row, string label, Control input)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); grid.Controls.Add(new Label { Text = label, ForeColor = Color.FromArgb(155, 190, 215), TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, row); grid.Controls.Add(input, 1, row);
    }

    private void ContinueGame()
    {
        try { if (string.IsNullOrWhiteSpace(profileName)) { using var ask = new TextPrompt("CONTINUAR PARTIDA", "Escribe el nombre exacto del perfil:"); if (ask.ShowDialog(this) != DialogResult.OK) return; profileName = ask.Value; } var list = repository.Listar(profileName); if (list.Count == 0) { MessageBox.Show("No hay partidas guardadas para el perfil actual. Crea una partida y guárdala con F5.", "NEXUS", MessageBoxButtons.OK, MessageBoxIcon.Information); return; } LoadSave(list[0].IdPartida); }
        catch (Exception ex) { ShowDatabaseError(ex); }
    }

    private void LoadGameDialog()
    {
        try
        {
            string name = profileName;
            if (string.IsNullOrWhiteSpace(name)) { using var ask = new TextPrompt("PERFIL DEL JUGADOR", "Escribe el nombre exacto del perfil:"); if (ask.ShowDialog(this) != DialogResult.OK) return; name = ask.Value; }
            var saves = repository.Listar(name);
            if (saves.Count == 0) { MessageBox.Show("No se encontraron partidas guardadas para ese perfil."); return; }
            using var dialog = new Form { Text = "SELECCIONAR PARTIDA", Size = new Size(620, 420), StartPosition = FormStartPosition.CenterParent, BackColor = panel, ForeColor = Color.White };
            var list = new ListBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(5, 12, 25), ForeColor = Color.White, Font = new Font("Consolas", 10) };
            foreach (var s in saves) list.Items.Add($"#{s.IdPartida}  NIVEL {s.Nivel:00}  {s.Estado}  {s.FechaGuardado:dd/MM/yyyy HH:mm}");
            list.SelectedIndex = 0; dialog.Controls.Add(list);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 54, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) }; dialog.Controls.Add(buttons);
            int selected = 0; var load = MakeButton("CARGAR", cyan, 120, 34); load.ForeColor = bg; load.Click += (_, _) => { selected = list.SelectedIndex; dialog.DialogResult = DialogResult.OK; dialog.Close(); }; buttons.Controls.Add(load);
            var cancel = MakeButton("CANCELAR", Color.FromArgb(35, 55, 80), 120, 34); cancel.Click += (_, _) => dialog.Close(); buttons.Controls.Add(cancel);
            var delete = MakeButton("ELIMINAR", Color.FromArgb(112, 43, 58), 120, 34); delete.Click += (_, _) => { if (list.SelectedIndex < 0) return; var chosen=saves[list.SelectedIndex]; if (MessageBox.Show($"¿Eliminar definitivamente la partida #{chosen.IdPartida}?", "CONFIRMAR", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)==DialogResult.Yes) { repository.Eliminar(chosen.IdPartida,name); saves.RemoveAt(list.SelectedIndex); list.Items.RemoveAt(list.SelectedIndex); if(list.Items.Count==0) { dialog.DialogResult=DialogResult.Cancel; dialog.Close(); } else list.SelectedIndex=0; } }; buttons.Controls.Add(delete);
            if (dialog.ShowDialog(this) == DialogResult.OK && selected >= 0) { profileName = name; LoadSave(saves[selected].IdPartida); }
        }
        catch (Exception ex) { ShowDatabaseError(ex); }
    }

    private void LoadSave(int id)
    {
        var save = repository.Cargar(id); if (save.Juego == null) throw new InvalidOperationException("La partida no contiene datos recuperables.");
        var config = repository.CargarConfiguracion(save.Jugador);
        if (config.HasValue) { difficulty = config.Value.Dificultad; soundEnabled = config.Value.Sonido; if (config.Value.PantallaCompleta) WindowState = FormWindowState.Maximized; }
        world = new GameWorld(save.Juego, save.Zonas, save.ZonaActiva, save.X, save.Y, difficulty, save.Enemigos, save.TiempoSegundos, save.Movimientos, save.Ataques);
        profileName = save.Jugador; currentSaveId = save.IdPartida; inventoryOpen = false; paused = false; overlay.Visible = false; RefreshHud(); PlaySound(); canvas.Focus();
    }

    private void SaveGame()
    {
        if (world == null) return;
        try { currentSaveId = repository.Guardar(profileName, world.Estado, world.Zonas, world.ZonaActiva, world.X, world.Y, currentSaveId, world.TiempoSegundos, world.Movimientos, world.Ataques, world.Enemigos); messageValue.Text = $"PARTIDA #{currentSaveId} GUARDADA EN SQL SERVER // {DateTime.Now:HH:mm:ss}"; }
        catch (Exception ex) { ShowDatabaseError(ex); }
    }

    private void ShowDatabaseError(Exception ex)
    {
        MessageBox.Show("No se pudo completar la operación de base de datos. NEXUS no simulará un guardado.\n\n" + ex.Message,
            "CONEXIÓN SQL SERVER", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ShowPauseMenu()
    {
        if (world == null) { ShowMainMenu(); return; }
        paused = true; heldKeys.Clear(); overlay.Controls.Clear(); overlay.Visible = true;
        var box = new FlowLayoutPanel { Size = new Size(420, 490), FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.FromArgb(245, 8, 18, 35), Padding = new Padding(34), Anchor = AnchorStyles.None };
        overlay.Controls.Add(box); box.Location = new Point((overlay.Width - box.Width) / 2, (overlay.Height - box.Height) / 2); overlay.Resize += (_, _) => box.Location = new Point((overlay.Width - box.Width) / 2, (overlay.Height - box.Height) / 2);
        var title = new Label { Text = "SISTEMA EN PAUSA", ForeColor = cyan, Font = new Font("Segoe UI", 22, FontStyle.Bold), Width = 350, Height = 60, TextAlign = ContentAlignment.MiddleCenter }; box.Controls.Add(title);
        AddMenuButton(box, "▶  CONTINUAR", () => { paused = false; overlay.Visible = false; canvas.Focus(); }, true);
        AddMenuButton(box, "▣  GUARDAR PARTIDA", () => SaveGame());
        AddMenuButton(box, "▷  CARGAR PARTIDA", () => LoadGameDialog());
        AddMenuButton(box, "◈  CONFIGURACIÓN", () => ShowSettings());
        AddMenuButton(box, "⌂  VOLVER AL MENÚ", () => ShowMainMenu());
    }

    private void ShowSettings()
    {
        using var dialog = new Form { Text = "CONFIGURACIÓN", Size = new Size(420, 260), StartPosition = FormStartPosition.CenterParent, BackColor = panel, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(24) }; dialog.Controls.Add(flow);
        var sound = new CheckBox { Text = "Audio activado", Checked = soundEnabled, ForeColor = Color.White, AutoSize = true }; flow.Controls.Add(sound);
        var full = new CheckBox { Text = "Pantalla completa", Checked = WindowState == FormWindowState.Maximized, ForeColor = Color.White, AutoSize = true }; flow.Controls.Add(full);
        var save = MakeButton("APLICAR", cyan, 150, 38); save.ForeColor = bg; save.Click += (_, _) => { soundEnabled = sound.Checked; if (full.Checked) WindowState = FormWindowState.Maximized; else WindowState = FormWindowState.Normal; if (!soundEnabled) music?.Stop(); else if (!paused) PlaySound(); SaveLocalSettings(); if (!string.IsNullOrWhiteSpace(profileName)) { try { repository.GuardarConfiguracion(profileName, difficulty, soundEnabled, full.Checked); } catch (Exception ex) { ShowDatabaseError(ex); } } dialog.Close(); }; flow.Controls.Add(save);
        dialog.ShowDialog(this);
    }

    private void LoadLocalSettings()
    {
        try
        {
            string path=Path.Combine(AppContext.BaseDirectory,"nexus-settings.json");
            if(!File.Exists(path)) return;
            using var document=JsonDocument.Parse(File.ReadAllText(path));
            if(document.RootElement.TryGetProperty("soundEnabled",out var sound)) soundEnabled=sound.GetBoolean();
            if(document.RootElement.TryGetProperty("difficulty",out var diff)) difficulty=diff.GetString() ?? "Normal";
        }
        catch { soundEnabled=true; difficulty="Normal"; }
    }

    private void SaveLocalSettings()
    {
        try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "nexus-settings.json"), JsonSerializer.Serialize(new { soundEnabled, difficulty })); }
        catch { }
    }

    private void ShowStatistics()
    {
        if (world == null) { MessageBox.Show("Inicia o carga una partida para consultar las estadísticas."); return; }
        var c = world.Estado.Cadete;
        MessageBox.Show($"OPERADOR: {c.Nombre}\nNIVEL: {world.Estado.Nivel}\nXP: {c.Xp}\nENERGÍA: {c.Energia:0}/{c.EnergiaMaxima:0}\nESTABILIDAD: {c.Estabilidad:0}\nENTIDADES DERROTADAS: {world.Estado.EntidadesDerrotadas}\nANOMALÍAS: {world.Estado.Anomalias}\nOBJETOS: {world.Estado.Inventario.Cantidad}/8", "ESTADÍSTICAS NEXUS");
    }

    private void ShowAchievements()
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            using var ask=new TextPrompt("LOGROS NEXUS","Escribe el nombre exacto del perfil:");
            if(ask.ShowDialog(this)!=DialogResult.OK) return;
            profileName=ask.Value;
        }
        try
        {
            var achievements=repository.ListarLogros(profileName);
            if(achievements.Count==0) { MessageBox.Show("Todavía no hay logros guardados para este perfil. Guarda una partida después de completar objetivos.","LOGROS"); return; }
            string text=string.Join(Environment.NewLine+Environment.NewLine,achievements.Select(a=>$"★ {a.Titulo}\n{a.Descripcion}\n{a.Fecha:dd/MM/yyyy HH:mm}"));
            MessageBox.Show(text,"REGISTRO DE LOGROS");
        }
        catch(Exception ex) { ShowDatabaseError(ex); }
    }

    private void ShowCredits() => MessageBox.Show("NEXUS // THE LOST ANOMALY\n\nProyecto original: NEXUS Training System\nTransformación 2D: arquitectura C# / Windows Forms\nMotor gráfico: GDI+ con renderizado de sprites vectoriales\nPersistencia: SQL Server mediante ADO.NET", "CRÉDITOS");

    private void AddMenuButton(Control parent, string text, Action action, bool primary = false)
    {
        var b = MakeButton(text, primary ? cyan : Color.FromArgb(24, 48, 73), 310, 43); b.ForeColor = primary ? bg : Color.White; b.Margin = new Padding(0, 5, 0, 5); b.Click += (_, _) => action(); parent.Controls.Add(b);
    }

    private Button MakeButton(string text, Color color, int width, int height)
    {
        var b = new Button { Text = text, Width = width, Height = height, FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand, UseVisualStyleBackColor = false };
        b.FlatAppearance.BorderColor = Color.FromArgb(54, 115, 153); b.FlatAppearance.BorderSize = 1; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(43, 89, 125); return b;
    }

    private void OnGameKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { if (overlay.Visible) { if (world != null) { paused = false; overlay.Visible = false; } else ShowMainMenu(); } else ShowPauseMenu(); e.Handled = true; return; }
        if (world != null && (world.GameOver || world.Victoria) && e.KeyCode == Keys.R) { string cadete=world.Estado.Cadete.Nombre; int edad=world.Estado.Cadete.Edad; string realidad=world.Estado.Cadete.Realidad; world = new GameWorld(cadete, edad, realidad, difficulty); currentSaveId = 0; overlay.Visible=false; paused=false; RefreshHud(); canvas.Invalidate(); e.Handled=true; return; }
        if (overlay.Visible || world == null) return;
        if (e.KeyCode == Keys.F5) { SaveGame(); e.Handled = true; return; }
        if (e.KeyCode == Keys.F9) { LoadGameDialog(); e.Handled = true; return; }
        if (e.KeyCode == Keys.F1) { world.CambiarZona(0); RefreshHud(); canvas.Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.F2) { world.CambiarZona(1); RefreshHud(); canvas.Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.F3) { world.CambiarZona(2); RefreshHud(); canvas.Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.I) { inventoryOpen = !inventoryOpen; canvas.Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.E) { world.Interactuar(); PlayEffect(interactionSound); RefreshHud(); canvas.Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.Space) { world.Atacar(); PlayEffect(actionSound); RefreshHud(); canvas.Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.Q) { world.HabilidadEspecial(); PlayEffect(actionSound); RefreshHud(); canvas.Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.D1) { world.UsarObjeto(0); RefreshHud(); canvas.Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.D2) { world.UsarObjeto(1); RefreshHud(); canvas.Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.D3) { world.UsarObjeto(2); RefreshHud(); canvas.Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.D4) { world.UsarObjeto(3); RefreshHud(); canvas.Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.D5) { world.UsarObjeto(4); RefreshHud(); canvas.Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.D6) { world.UsarObjeto(5); RefreshHud(); canvas.Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.D7) { world.UsarObjeto(6); RefreshHud(); canvas.Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.D8) { world.UsarObjeto(7); RefreshHud(); canvas.Invalidate(); e.Handled = true; return; }
        heldKeys.Add(e.KeyCode);
    }

    private void UpdateGame()
    {
        if (world == null || paused || overlay.Visible) return;
        var now = DateTime.UtcNow;
        double movementInterval = heldKeys.Contains(Keys.ShiftKey) ? 75 : 130;
        if ((now - lastFrame).TotalMilliseconds < movementInterval) return;
        lastFrame = now;
        world.Tick();
        if (heldKeys.Contains(Keys.W) || heldKeys.Contains(Keys.Up)) world.Mover(0, -1);
        else if (heldKeys.Contains(Keys.S) || heldKeys.Contains(Keys.Down)) world.Mover(0, 1);
        else if (heldKeys.Contains(Keys.A) || heldKeys.Contains(Keys.Left)) world.Mover(-1, 0);
        else if (heldKeys.Contains(Keys.D) || heldKeys.Contains(Keys.Right)) world.Mover(1, 0);
        RefreshHud(); canvas.Invalidate();
    }

    private void RefreshHud()
    {
        if (world == null) return;
        var c = world.Estado.Cadete;
        playerValue.Text = c.Nombre.ToUpperInvariant(); levelValue.Text = $"NIVEL {world.Estado.Nivel:00}  //  RANGO {c.Rango}";
        energyBar.Value = Math.Clamp((int)c.Energia, 0, 100); energyValue.Text = $"{c.Energia:0} / {c.EnergiaMaxima:0}";
        stabilityBar.Value = Math.Clamp((int)c.Estabilidad, 0, 100); stabilityValue.Text = $"{c.Estabilidad:0} / 100";
        shieldValue.Text = $"ESCUDO  {c.Escudo:0}    MÓDULO  {c.CargasModulo}"; xpValue.Text = $"XP  {c.Xp} / {(c.Rango + 1) * 100}";
        zoneValue.Text = $"{world.ZonaActual.Nombre}\n{world.ZonaActual.Titulo}\nX:{world.X:00}  Y:{world.Y:00}";
        var m = world.Estado.MisionPrincipal;
        missionValue.Text = m.Titulo + "\n\n" + string.Join("\n", m.Objetivos.Select(o => (o.Completado ? "✓ " : "○ ") + o.Descripcion));
        inventoryValue.Text = string.Join("\n", world.Estado.Inventario.Lista.Select((o, i) => $"[{i + 1}] {o.Icono} {o.Nombre}"));
        if (string.IsNullOrWhiteSpace(inventoryValue.Text)) inventoryValue.Text = "INVENTARIO VACÍO";
        messageValue.Text = world.Mensaje;
        if (world.GameOver) { paused = true; ShowEndScreen(false); }
        else if (world.Victoria) { paused = true; ShowEndScreen(true); }
    }

    private void ShowEndScreen(bool win)
    {
        overlay.Controls.Clear(); overlay.Visible = true;
        var box = new FlowLayoutPanel { Size = new Size(500, 320), FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.FromArgb(245, 8, 18, 35), Padding = new Padding(28) };
        overlay.Controls.Add(box); box.Location = new Point((overlay.Width - box.Width) / 2, (overlay.Height - box.Height) / 2);
        box.Controls.Add(new Label { Text = win ? "MISIÓN COMPLETADA" : "SEÑAL PERDIDA", ForeColor = win ? cyan : Color.OrangeRed, Font = new Font("Segoe UI", 24, FontStyle.Bold), Width = 430, Height = 70, TextAlign = ContentAlignment.MiddleCenter });
        box.Controls.Add(new Label { Text = win ? "La anomalía ha sido contenida.\nTu señal vuelve a estar estable." : "El cadete ha caído en el sector.\nLa expedición ha terminado.", ForeColor = Color.White, Width = 430, Height = 65, TextAlign = ContentAlignment.MiddleCenter });
        AddMenuButton(box, "GUARDAR RESULTADO", () => SaveGame());
        AddMenuButton(box, "VOLVER AL MENÚ", () => ShowMainMenu());
        AddMenuButton(box, "NUEVA EXPEDICIÓN", () => ShowNewGameDialog(), true);
    }

    private void PlaySound()
    {
        if (!soundEnabled) return;
        try { music?.PlayLooping(); sfx?.Play(); } catch { }
    }

    private void PlayEffect(SoundPlayer? effect)
    {
        if (!soundEnabled || effect == null) return;
        try { effect.Play(); } catch { }
    }

    private void RefreshCanvas() => canvas.Invalidate();

    private sealed class TextPrompt : Form
    {
        private readonly TextBox input = new();
        public string Value => input.Text.Trim();
        public TextPrompt(string title, string prompt)
        {
            Text=title; Size=new Size(420,180); StartPosition=FormStartPosition.CenterParent; BackColor=Color.FromArgb(13,24,43); ForeColor=Color.White; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false;
            var label=new Label{Text=prompt,Dock=DockStyle.Top,Height=42,Padding=new Padding(16,12,0,0)};Controls.Add(label);
            input.Dock=DockStyle.Top;input.Margin=new Padding(16);input.BackColor=Color.FromArgb(5,12,25);input.ForeColor=Color.White;Controls.Add(input);
            var ok=new Button{Text="ACEPTAR",Dock=DockStyle.Bottom,Height=38,DialogResult=DialogResult.OK};Controls.Add(ok);AcceptButton=ok;
        }
    }

    private sealed class GameCanvas : Panel
    {
        private readonly Func<GameWorld?> getWorld;
        private readonly Func<bool> inventory;
        private readonly Func<bool> paused;
        public GameCanvas(Func<GameWorld?> getWorld, Func<bool> inventory, Func<bool> paused) { this.getWorld=getWorld;this.inventory=inventory;this.paused=paused;DoubleBuffered=true; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); var g=e.Graphics; g.SmoothingMode=SmoothingMode.AntiAlias; g.Clear(Color.FromArgb(4,9,18));
            DrawStars(g, ClientRectangle);
            var world=getWorld(); if(world==null) { DrawWaiting(g); return; }
            int tile=Math.Clamp(Math.Min((ClientSize.Width-40)/20,(ClientSize.Height-60)/14),20,30);
            int visibleCols=Math.Clamp((ClientSize.Width-30)/tile,1,Zona.Ancho);
            int visibleRows=Math.Clamp((ClientSize.Height-55)/tile,1,Zona.Alto);
            int cameraX=Math.Clamp(world.X-visibleCols/2,0,Zona.Ancho-visibleCols);
            int cameraY=Math.Clamp(world.Y-visibleRows/2,0,Zona.Alto-visibleRows);
            int ox=(ClientSize.Width-visibleCols*tile)/2, oy=(ClientSize.Height-visibleRows*tile)/2+4;
            DrawMap(g,world,ox,oy,tile,cameraX,cameraY,visibleCols,visibleRows);
            DrawEnemies(g,world,ox,oy,tile,cameraX,cameraY,visibleCols,visibleRows);
            DrawPlayer(g,world,ox,oy,tile,cameraX,cameraY);
            DrawAttackParticles(g,world,ox,oy,tile,cameraX,cameraY);
            DrawCoordinates(g,world,ox,oy,tile,visibleRows);
            if(inventory()) DrawInventory(g,world);
            if(paused()) DrawPaused(g);
        }
        private static void DrawStars(Graphics g,Rectangle r)
        {
            using var star=new SolidBrush(Color.FromArgb(95,95,165,205));
            for(int i=0;i<90;i++){int x=(i*137+43)%Math.Max(1,r.Width);int y=(i*79+17)%Math.Max(1,r.Height);g.FillEllipse(star,x,y,1.5f,1.5f);}
        }
        private static void DrawWaiting(Graphics g)
        {
            using var f=new Font("Segoe UI",24,FontStyle.Bold);using var b=new SolidBrush(Color.FromArgb(48,210,245));
            g.DrawString("NEXUS // IRIS SECTOR",f,b,new PointF(35,45));
            using var sf=new Font("Consolas",11);using var sb=new SolidBrush(Color.FromArgb(145,180,210));
            g.DrawString("ESPERANDO INICIALIZACIÓN DE EXPEDICIÓN...",sf,sb,new PointF(40,105));
        }
        private static void DrawMap(Graphics g,GameWorld w,int ox,int oy,int t,int camX,int camY,int cols,int rows)
        {
            var map=w.ZonaActual.Mapa;
            for(int sy=0;sy<rows;sy++)for(int sx=0;sx<cols;sx++)
            {
                int x=camX+sx,y=camY+sy; char c=map[y,x]; var rect=new Rectangle(ox+sx*t,oy+sy*t,t,t);
                using var brush=new SolidBrush(TileColor(c));g.FillRectangle(brush,rect);
                using var pen=new Pen(Color.FromArgb(35,100,160,190));g.DrawRectangle(pen,rect);
                if(c is '◆' or '⚠' or '⌂' or '✦' or '◇') DrawTileGlyph(g,c,rect);
            }
        }
        private static Color TileColor(char c) => c switch
        {
            '^'=>Color.FromArgb(43,55,73),'~'=>Color.FromArgb(17,68,104),'#'=>Color.FromArgb(21,70,57),'-'=>Color.FromArgb(81,77,48),
            '?'=>Color.FromArgb(24,33,49),'▓'=>Color.FromArgb(44,39,67),'◆'=>Color.FromArgb(23,76,90),'⚠'=>Color.FromArgb(85,38,50),
            '●'=>Color.FromArgb(66,34,65),'◉'=>Color.FromArgb(39,53,80),'⌂'=>Color.FromArgb(33,74,78),'✦'=>Color.FromArgb(80,63,36),'◇'=>Color.FromArgb(43,60,81),
            _=>Color.FromArgb(19,35,45)
        };
        private static void DrawTileGlyph(Graphics g,char c,Rectangle r)
        {
            string s=c.ToString();using var f=new Font("Segoe UI Symbol",Math.Max(10,r.Width*.48f),FontStyle.Bold);using var b=new SolidBrush(c=='⚠'?Color.OrangeRed:c=='◆'?Color.Cyan:c=='⌂'?Color.White:Color.FromArgb(225,206,255));
            var size=g.MeasureString(s,f);g.DrawString(s,f,b,r.X+(r.Width-size.Width)/2,r.Y+(r.Height-size.Height)/2);
        }
        private static void DrawEnemies(Graphics g,GameWorld w,int ox,int oy,int t,int camX,int camY,int cols,int rows)
        {
            foreach(var enemy in w.Enemigos.Where(e=>!e.Derrotado && e.X>=camX && e.X<camX+cols && e.Y>=camY && e.Y<camY+rows))
            {
                float x=ox+(enemy.X-camX)*t+t*.18f,y=oy+(enemy.Y-camY)*t+t*.18f;float size=t*.64f;
                Color c=enemy.Entidad is Enemigo en && en.EsJefe?Color.FromArgb(220,60,90):enemy.Entidad is Robot?Color.FromArgb(255,172,66):Color.FromArgb(194,66,225);
                using var glow=new SolidBrush(Color.FromArgb(45,c));g.FillEllipse(glow,x-4,y-4,size+8,size+8);
                using var b=new SolidBrush(c);g.FillEllipse(b,x,y,size,size);
                using var eye=new SolidBrush(Color.White);g.FillEllipse(eye,x+size*.25f,y+size*.28f,size*.16f,size*.13f);g.FillEllipse(eye,x+size*.59f,y+size*.28f,size*.16f,size*.13f);
                using var hpbg=new SolidBrush(Color.FromArgb(120,0,0,0));g.FillRectangle(hpbg,x-2,y-7,size+4,4);
                using var hp=new SolidBrush(Color.FromArgb(255,100,105));g.FillRectangle(hp,x-1,y-6,(size-2)*(float)(enemy.Entidad.Energia/enemy.Entidad.EnergiaMaxima),2);
            }
        }
        private static void DrawPlayer(Graphics g,GameWorld w,int ox,int oy,int t,int camX,int camY)
        {
            float bob=(float)Math.Sin(Environment.TickCount/110.0)*1.6f;float x=ox+(w.RenderX-camX)*t+t*.2f,y=oy+(w.RenderY-camY)*t+t*.12f+bob,size=t*.6f;
            using var shadow=new SolidBrush(Color.FromArgb(100,0,0,0));g.FillEllipse(shadow,x-3,y+size*.82f,size+6,size*.28f);
            using var suit=new SolidBrush(Color.FromArgb(35,150,195));g.FillRoundedRectangle(suit,new RectangleF(x+size*.18f,y+size*.34f,size*.64f,size*.54f),5);
            using var helmet=new SolidBrush(Color.FromArgb(205,235,247));g.FillEllipse(helmet,x+size*.22f,y,size*.56f,size*.52f);
            using var visor=new SolidBrush(Color.FromArgb(13,46,72));g.FillEllipse(visor,x+size*.29f,y+size*.14f,size*.42f,size*.23f);
            using var pack=new SolidBrush(Color.FromArgb(150,92,255));g.FillRectangle(pack,x+size*.03f,y+size*.42f,size*.18f,size*.38f);
            using var outline=new Pen(Color.FromArgb(70,225,255),1.5f);g.DrawEllipse(outline,x+size*.22f,y,size*.56f,size*.52f);
        }
        private static void DrawAttackParticles(Graphics g,GameWorld w,int ox,int oy,int t,int camX,int camY)
        {
            if(!w.MostrarEfectoAtaque || w.EfectoAtaqueX<camX || w.EfectoAtaqueX>=camX+Zona.Ancho || w.EfectoAtaqueY<camY || w.EfectoAtaqueY>=camY+Zona.Alto) return;
            float cx=ox+(w.EfectoAtaqueX-camX)*t+t/2f, cy=oy+(w.EfectoAtaqueY-camY)*t+t/2f;
            using var glow=new SolidBrush(Color.FromArgb(110,48,220,255));using var core=new SolidBrush(Color.FromArgb(235,220,250,255));
            for(int i=0;i<9;i++) { double a=i*Math.PI*2/9+Environment.TickCount/90.0;float radius=t*.42f;float px=cx+(float)Math.Cos(a)*radius,py=cy+(float)Math.Sin(a)*radius;g.FillEllipse(glow,px-3,py-3,6,6);g.FillEllipse(core,px-1.5f,py-1.5f,3,3); }
        }

        private static void DrawCoordinates(Graphics g,GameWorld w,int ox,int oy,int t,int visibleRows)
        {
            using var f=new Font("Consolas",9,FontStyle.Bold);using var b=new SolidBrush(Color.FromArgb(170,220,245));
            g.DrawString($"{w.ZonaActual.Nombre}  //  POS {w.X:00}:{w.Y:00}",f,b,ox,oy-22);
            g.DrawString("WASD MOVER  •  E INTERACTUAR  •  ESPACIO ATAQUE",f,b,ox,oy+visibleRows*t+5);
        }
        private static void DrawInventory(Graphics g,GameWorld w)
        {
            var r=new Rectangle(35,35,(int)Math.Min(390,g.VisibleClipBounds.Width-70),(int)Math.Min(350,g.VisibleClipBounds.Height-70));
            using var b=new SolidBrush(Color.FromArgb(238,7,16,31));using var p=new Pen(Color.FromArgb(80,210,255),2);g.FillRectangle(b,r);g.DrawRectangle(p,r);
            using var title=new Font("Segoe UI",16,FontStyle.Bold);using var body=new Font("Consolas",10);using var white=new SolidBrush(Color.White);using var cyan=new SolidBrush(Color.FromArgb(60,220,255));
            g.DrawString("INVENTARIO // 8 SLOTS",title,cyan,r.X+18,r.Y+15);
            int i=0;foreach(var item in w.Estado.Inventario.Lista){g.DrawString($"[{i+1}]  {item.Icono}  {item.Nombre}",body,white,r.X+20,r.Y+62+i*28);i++;}
            g.DrawString("Pulsa 1–4 para usar un objeto.",body,cyan,r.X+20,r.Bottom-35);
        }
        private static void DrawPaused(Graphics g)
        {
            using var b=new SolidBrush(Color.FromArgb(90,0,0,0));g.FillRectangle(b,g.VisibleClipBounds);
            using var f=new Font("Segoe UI",22,FontStyle.Bold);using var c=new SolidBrush(Color.FromArgb(55,220,255));
            var text="SISTEMA EN PAUSA";var size=g.MeasureString(text,f);g.DrawString(text,f,c,(g.VisibleClipBounds.Width-size.Width)/2,(g.VisibleClipBounds.Height-size.Height)/2);
        }
    }
}
