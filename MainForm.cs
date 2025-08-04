using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WinFormsTimer = System.Windows.Forms.Timer;
using ThreadingTimer = System.Threading.Timer;

namespace EscapeGameControllerGUI
{
    public partial class MainForm : Form
    {
        private TcpListener server;
        private List<TcpClient> connectedClients = new List<TcpClient>();
        private bool isRunning = false;
        private Thread serverThread;

        // Scènes disponibles
        private readonly List<string> availableScenes = new List<string>
        {
            "Menu", "ChoixPersonnage", "Platformer", "Lock",
            "Labo+Dragon", "EspionAnimation", "Donjon", "SpaceInvader",
            "Boss", "EspaceAnimation", "ChambreAnimation"
        };

        private List<string> currentSceneOrder = new List<string>
        {
            "Menu", "ChoixPersonnage", "Platformer", "Lock",
            "Labo+Dragon", "EspionAnimation", "SpaceInvader",
            "Boss", "EspaceAnimation", "ChambreAnimation"
        };
        private int currentSceneIndex = 0;

        // Contrôles UI
        private Button btnStartGame;
        private Button btnNextScene;
        private Button btnResetGame;
        private Label lblClientsConnected;
        private Label lblCurrentScene;
        private ListBox lstAvailableScenes;
        private ListBox lstSceneOrder;
        private Button btnAddScene;
        private Button btnRemoveScene;
        private Button btnMoveUp;
        private Button btnMoveDown;
        private Button btnClearOrder;
        private RichTextBox txtLog;
        private ProgressBar progressGame;
        private Panel sceneCountCard;
        private Panel progressCard;

        public MainForm()
        {
            InitializeComponent();
            LoadDefaultSceneOrder();
            UpdateUI();
            StartServer();
        }
        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configuration de la fenêtre principale - Style clair et moderne
            this.Text = "ESCAPE GAME MASTER CONTROL";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // Barre de titre personnalisée
            Panel titleBar = new Panel();
            titleBar.Size = new Size(1400, 50);
            titleBar.Location = new Point(0, 0);
            titleBar.BackColor = Color.White;
            titleBar.MouseDown += TitleBar_MouseDown;

            // Ombre pour la barre de titre
            titleBar.Paint += (s, e) =>
            {
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(shadowBrush, 0, titleBar.Height - 2, titleBar.Width, 2);
                }
            };

            Label titleLabel = new Label();
            titleLabel.Text = "⚡ ESCAPE GAME MASTER CONTROL";
            titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(52, 58, 64);
            titleLabel.Location = new Point(20, 15);
            titleLabel.Size = new Size(400, 25);

            Button closeBtn = new Button();
            closeBtn.Text = "×";
            closeBtn.Size = new Size(50, 50);
            closeBtn.Location = new Point(1350, 0);
            closeBtn.BackColor = Color.Transparent;
            closeBtn.ForeColor = Color.FromArgb(108, 117, 125);
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            closeBtn.Click += (s, e) => this.Close();
            closeBtn.MouseEnter += (s, e) => closeBtn.BackColor = Color.FromArgb(220, 53, 69);
            closeBtn.MouseEnter += (s, e) => closeBtn.ForeColor = Color.White;
            closeBtn.MouseLeave += (s, e) => closeBtn.BackColor = Color.Transparent;
            closeBtn.MouseLeave += (s, e) => closeBtn.ForeColor = Color.FromArgb(108, 117, 125);

            titleBar.Controls.AddRange(new Control[] { titleLabel, closeBtn });

            // Header avec statistiques - Style dashboard clair
            Panel headerPanel = CreateLightPanel(new Point(20, 70), new Size(1360, 120));

            // Status cards modernes et claires
            Panel connectionCard = CreateStatusCard("CONNEXIONS", "0", Color.FromArgb(40, 167, 69), new Point(30, 20));
            sceneCountCard = CreateStatusCard("SCÈNES", currentSceneOrder.Count.ToString(), Color.FromArgb(255, 193, 7), new Point(280, 20));
            progressCard = CreateStatusCard("PROGRESSION", "0%", Color.FromArgb(0, 123, 255), new Point(530, 20));

            lblClientsConnected = (Label)connectionCard.Controls[1]; // Récupération du label de valeur

            headerPanel.Controls.AddRange(new Control[] { connectionCard, sceneCountCard, progressCard });

            // Section principale avec layout en grille
            Panel mainGrid = new Panel();
            mainGrid.Location = new Point(20, 210);
            mainGrid.Size = new Size(1360, 500);
            mainGrid.BackColor = Color.Transparent;

            // Panel configuration scènes - Style propre
            Panel scenesPanel = CreateLightPanel(new Point(0, 0), new Size(650, 500));

            Label scenesTitle = new Label();
            scenesTitle.Text = "⚙️ CONFIGURATION DES SCÈNES";
            scenesTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            scenesTitle.ForeColor = Color.FromArgb(52, 58, 64);
            scenesTitle.BackColor = Color.FromArgb(245, 245, 245);
            scenesTitle.Location = new Point(25, 25);
            scenesTitle.Size = new Size(400, 25);

            // Listes avec style clair
            lstAvailableScenes = CreateLightListBox(new Point(25, 80), new Size(250, 300));
            lstAvailableScenes.Items.AddRange(availableScenes.ToArray());

            lstSceneOrder = CreateLightListBox(new Point(375, 80), new Size(250, 300));

            Label availableLabel = CreateLabel("DISPONIBLES", new Point(25, 60), Color.FromArgb(40, 167, 69));
            Label orderLabel = CreateLabel("SÉQUENCE", new Point(375, 60), Color.FromArgb(255, 193, 7));

            // Boutons de contrôle avec style propre
            Panel controlButtons = new Panel();
            controlButtons.Location = new Point(290, 150);
            controlButtons.Size = new Size(70, 200);
            controlButtons.BackColor = Color.Transparent;

            btnAddScene = CreateLightButton("▶", new Point(0, 0), new Size(60, 40), Color.FromArgb(40, 167, 69));
            btnRemoveScene = CreateLightButton("◀", new Point(0, 50), new Size(60, 40), Color.FromArgb(220, 53, 69));
            btnMoveUp = CreateLightButton("▲", new Point(0, 100), new Size(60, 40), Color.FromArgb(0, 123, 255));
            btnMoveDown = CreateLightButton("▼", new Point(0, 150), new Size(60, 40), Color.FromArgb(0, 123, 255));

            btnAddScene.Click += BtnAddScene_Click;
            btnRemoveScene.Click += BtnRemoveScene_Click;
            btnMoveUp.Click += BtnMoveUp_Click;
            btnMoveDown.Click += BtnMoveDown_Click;

            controlButtons.Controls.AddRange(new Control[] { btnAddScene, btnRemoveScene, btnMoveUp, btnMoveDown });

            btnClearOrder = CreateLightButton("RESET", new Point(375, 400), new Size(100, 40), Color.FromArgb(220, 53, 69));
            btnClearOrder.Click += BtnClearOrder_Click;

            scenesPanel.Controls.AddRange(new Control[] {
                scenesTitle, lstAvailableScenes, lstSceneOrder,
                availableLabel, orderLabel, controlButtons, btnClearOrder
            });

            // Panel contrôle de jeu - Style command center clair
            Panel controlPanel = CreateLightPanel(new Point(670, 0), new Size(690, 500));

            Label controlTitle = new Label();
            controlTitle.Text = "🎮 CONTRÔLE DE JEU";
            controlTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            controlTitle.ForeColor = Color.FromArgb(52, 58, 64);
            controlTitle.BackColor = Color.FromArgb(245, 245, 245);
            controlTitle.Location = new Point(25, 25);
            controlTitle.Size = new Size(400, 25);

            // Boutons de contrôle principaux - Style clair
            btnStartGame = CreateMainButton("🚀 MET A JOUR SCENE", new Point(25, 80), new Size(200, 60), Color.FromArgb(40, 167, 69));
            btnNextScene = CreateMainButton("⏭️ SUIVANT", new Point(245, 80), new Size(200, 60), Color.FromArgb(0, 123, 255));
            btnResetGame = CreateMainButton("🔄 RESET", new Point(465, 80), new Size(200, 60), Color.FromArgb(255, 193, 7));

            btnStartGame.Click += BtnStartGame_Click;
            btnNextScene.Click += BtnNextScene_Click;
            btnResetGame.Click += BtnResetGame_Click;

            // Panneau de statut avancé
            Panel statusPanel = CreateLightPanel(new Point(25, 170), new Size(640, 120));

            lblCurrentScene = new Label();
            lblCurrentScene.Text = "En attente...";
            lblCurrentScene.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblCurrentScene.ForeColor = Color.FromArgb(52, 58, 64);
            lblCurrentScene.BackColor = Color.FromArgb(245, 245, 245);
            lblCurrentScene.Location = new Point(20, 20);
            lblCurrentScene.Size = new Size(600, 30);

            progressGame = CreateLightProgressBar(new Point(20, 65), new Size(600, 30));

            statusPanel.Controls.AddRange(new Control[] { lblCurrentScene, progressGame });

            controlPanel.Controls.AddRange(new Control[] {
                controlTitle, btnStartGame, btnNextScene, btnResetGame, statusPanel
            });

            mainGrid.Controls.AddRange(new Control[] { scenesPanel, controlPanel });

            // Console de log - Style terminal clair
            Panel logPanel = CreateLightPanel(new Point(20, 730), new Size(1360, 160));

            Label logTitle = new Label();
            logTitle.Text = "📟 CONSOLE";
            logTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            logTitle.BackColor = Color.FromArgb(245, 245, 245);
            logTitle.ForeColor = Color.FromArgb(40, 167, 69);
            logTitle.Location = new Point(25, 15);
            logTitle.Size = new Size(200, 25);

            txtLog = new RichTextBox();
            txtLog.Location = new Point(25, 45);
            txtLog.Size = new Size(1310, 100);
            txtLog.BackColor = Color.FromArgb(248, 249, 250);
            txtLog.ForeColor = Color.FromArgb(52, 58, 64);
            txtLog.BorderStyle = BorderStyle.FixedSingle;
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.ReadOnly = true;

            logPanel.Controls.AddRange(new Control[] { logTitle, txtLog });

            // Assemblage final
            this.Controls.AddRange(new Control[] { titleBar, headerPanel, mainGrid, logPanel });

            this.ResumeLayout(false);
        }

        private Panel CreateLightPanel(Point location, Size size)
        {
            Panel panel = new Panel();
            panel.Location = location;
            panel.Size = size;
            panel.BackColor = Color.White;

            panel.Paint += (s, e) =>
            {
                // Bordure subtile
                using (Pen borderPen = new Pen(Color.FromArgb(233, 236, 239), 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, panel.Width - 1, panel.Height - 1);
                }

                // Ombre légère
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(10, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(shadowBrush, 2, 2, panel.Width - 2, panel.Height - 2);
                }
            };

            return panel;
        }

        private Panel CreateStatusCard(string title, string value, Color accentColor, Point location)
        {
            Panel card = new Panel();
            card.Location = location;
            card.Size = new Size(220, 85);
            card.BackColor = Color.White;

            card.Paint += (s, e) =>
            {
                // Bordure colorée en haut
                using (SolidBrush brush = new SolidBrush(accentColor))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, card.Width, 4);
                }

                // Bordure générale
                using (Pen pen = new Pen(Color.FromArgb(233, 236, 239), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            Label titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            titleLabel.ForeColor = Color.FromArgb(108, 117, 125);
            titleLabel.Location = new Point(15, 15);
            titleLabel.Size = new Size(190, 18);

            Label valueLabel = new Label();
            valueLabel.Text = value;
            valueLabel.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            valueLabel.ForeColor = accentColor;
            valueLabel.Location = new Point(15, 35);
            valueLabel.Size = new Size(190, 45);

            card.Controls.AddRange(new Control[] { titleLabel, valueLabel });
            return card;
        }

        private ListBox CreateLightListBox(Point location, Size size)
        {
            ListBox listBox = new ListBox();
            listBox.Location = location;
            listBox.Size = size;
            listBox.BackColor = Color.FromArgb(248, 249, 250);
            listBox.ForeColor = Color.FromArgb(52, 58, 64);
            listBox.BorderStyle = BorderStyle.FixedSingle;
            listBox.Font = new Font("Segoe UI", 9F);
            listBox.DrawMode = DrawMode.OwnerDrawFixed;
            listBox.ItemHeight = 28;

            listBox.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;

                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                
                // Récupérer le texte original de l'item
                string itemText = ((ListBox)s).Items[e.Index].ToString();
                bool isMenu = itemText.Contains("Menu");
                
                // Couleurs différentes pour Menu
                Color backgroundColor;
                Color textColor;
                
                if (isMenu && s == lstSceneOrder)
                {
                    // Style spécial pour Menu dans la liste de séquence (couleur dorée/jaune)
                    backgroundColor = isSelected ? Color.FromArgb(255, 193, 7) : Color.FromArgb(255, 248, 220);
                    textColor = isSelected ? Color.White : Color.FromArgb(133, 100, 4);
                }
                else
                {
                    // Style normal pour les autres scènes
                    backgroundColor = isSelected ? Color.FromArgb(0, 123, 255) : Color.FromArgb(248, 249, 250);
                    textColor = isSelected ? Color.White : Color.FromArgb(52, 58, 64);
                }

                using (SolidBrush brush = new SolidBrush(backgroundColor))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }

                // Modifier le texte affiché pour Menu dans la liste de séquence
                string displayText = itemText;

                using (SolidBrush textBrush = new SolidBrush(textColor))
                {
                    e.Graphics.DrawString(displayText, listBox.Font, textBrush, e.Bounds.X + 10, e.Bounds.Y + 6);
                }
            };

            return listBox;
        }

        private Label CreateLabel(string text, Point location, Color color)
        {
            Label label = new Label();
            label.Text = text;
            label.BackColor = Color.FromArgb(245, 245, 245);
            label.Location = location;
            label.Size = new Size(200, 20);
            label.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label.ForeColor = color;
            return label;
        }

        private Button CreateLightButton(string text, Point location, Size size, Color color)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = location;
            btn.Size = size;
            btn.BackColor = Color.White;
            btn.ForeColor = color;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = color;
            btn.FlatAppearance.BorderSize = 2;
            btn.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;

            btn.MouseEnter += (s, e) =>
            {
                btn.BackColor = color;
                btn.ForeColor = Color.White;
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = Color.White;
                btn.ForeColor = color;
            };

            return btn;
        }

        private Button CreateMainButton(string text, Point location, Size size, Color baseColor)
        {
            Button btn = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btn.FlatAppearance.BorderSize = 0;

            // Couleurs
            Color originalColor = baseColor;
            Color hoverColor = Color.FromArgb(
                Math.Min(255, baseColor.R + 30),
                Math.Min(255, baseColor.G + 30),
                Math.Min(255, baseColor.B + 30)
            );

            bool isHovered = false;

            btn.MouseEnter += (s, e) =>
            {
                isHovered = true;
                btn.Invalidate(); // Force un redraw
            };

            btn.MouseLeave += (s, e) =>
            {
                isHovered = false;
                btn.Invalidate(); // Force un redraw
            };

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Ombre
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(shadowBrush, 2, 2, btn.Width - 4, btn.Height - 4);
                }

                // Fond
                using (SolidBrush brush = new SolidBrush(isHovered ? hoverColor : originalColor))
                {
                    Rectangle rect = new Rectangle(0, 0, btn.Width - 2, btn.Height - 2);
                    e.Graphics.FillRectangle(brush, rect);
                }

                // Texte
                TextRenderer.DrawText(
                    e.Graphics,
                    btn.Text,
                    btn.Font,
                    btn.ClientRectangle,
                    btn.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );
            };

            return btn;
        }

        private ProgressBar CreateLightProgressBar(Point location, Size size)
        {
            ProgressBar pb = new ProgressBar();
            pb.Location = location;
            pb.Size = size;
            pb.Style = ProgressBarStyle.Continuous;
            pb.BackColor = Color.FromArgb(233, 236, 239);
            pb.ForeColor = Color.FromArgb(40, 167, 69);
            return pb;
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Permettre le déplacement de la fenêtre
                const int WM_NCLBUTTONDOWN = 0xA1;
                const int HT_CAPTION = 0x2;

                User32.ReleaseCapture();
                User32.SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        private void StartServer()
        {
            try
            {
                server = new TcpListener(IPAddress.Any, 12345);
                server.Start();
                isRunning = true;

                serverThread = new Thread(AcceptClients);
                serverThread.Start();

                LogMessage("🟢 Serveur démarré avec succès", Color.FromArgb(40, 167, 69));
                UpdateGameControls();
            }
            catch (Exception ex)
            {
                LogMessage($"🔴 Erreur serveur: {ex.Message}", Color.FromArgb(220, 53, 69));
                MessageBox.Show($"Erreur lors du démarrage du serveur: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStartGame_Click(object sender, EventArgs e)
        {
            if (currentSceneOrder.Count == 0)
            {
                MessageBox.Show("Aucune scène définie ! Configurez d'abord l'ordre des scènes.", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            currentSceneIndex = 0;
            string orderData = string.Join(",", currentSceneOrder);
            SendToAllClients($"SET_SCENE_ORDER:{orderData}");

            LogMessage($"🎮 Jeu démarré avec {currentSceneOrder.Count} scènes", Color.FromArgb(0, 123, 255));
            UpdateGameStatus();
        }

        private void BtnNextScene_Click(object sender, EventArgs e)
        {
            if (currentSceneIndex < currentSceneOrder.Count - 1)
            {
                SendToAllClients("NEXT_SCENE");
                currentSceneIndex++;
                LogMessage($"⏭️ Passage à: {currentSceneOrder[currentSceneIndex]}", Color.FromArgb(0, 123, 255));
            }
            else
            {
                SendToAllClients("END_GAME");
                LogMessage("🏁 Jeu terminé", Color.FromArgb(255, 193, 7));
            }
            UpdateGameStatus();
        }

        private void BtnResetGame_Click(object sender, EventArgs e)
        {
            SendToAllClients("RESET_GAME");
            currentSceneIndex = 0;
            LogMessage("🔄 Jeu réinitialisé", Color.FromArgb(255, 193, 7));
            UpdateGameStatus();
        }

        private void BtnAddScene_Click(object sender, EventArgs e)
        {
            if (lstAvailableScenes.SelectedItem != null)
            {
                string selectedScene = lstAvailableScenes.SelectedItem.ToString();
                currentSceneOrder.Add(selectedScene);
                UpdateSceneOrderList();
                LogMessage($"➕ Scène ajoutée: {selectedScene}", Color.FromArgb(40, 167, 69));
            }
        }

        private void BtnRemoveScene_Click(object sender, EventArgs e)
        {
            if (lstSceneOrder.SelectedIndex >= 0)
            {
                int selectedIndex = lstSceneOrder.SelectedIndex;
                string selectedScene = currentSceneOrder[selectedIndex];
                
                // Vérifier si c'est Menu
                if (selectedScene == "Menu")
                {
                    return;
                }
                
                // Code existant pour supprimer
                currentSceneOrder.RemoveAt(selectedIndex);
                UpdateSceneOrderList();
                LogMessage($"➖ Scène supprimée: {selectedScene}", Color.FromArgb(220, 53, 69));
            }
        }

        private void BtnMoveUp_Click(object sender, EventArgs e)
        {
            int selectedIndex = lstSceneOrder.SelectedIndex;
            if (selectedIndex > 0)
            {
                string scene = currentSceneOrder[selectedIndex];
                
                // Empêcher de déplacer Menu
                if (scene == "Menu")
                {
                    MessageBox.Show("La scène Menu doit rester en première position !", 
                                "Action interdite", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Warning);
                    return;
                }
                
                // Empêcher de placer une scène avant Menu
                if (selectedIndex == 1 && currentSceneOrder[0] == "Menu")
                {
                    MessageBox.Show("Aucune scène ne peut être placée avant Menu !", 
                                "Action interdite", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Warning);
                    return;
                }
                
                // Code existant pour déplacer vers le haut
                currentSceneOrder.RemoveAt(selectedIndex);
                currentSceneOrder.Insert(selectedIndex - 1, scene);
                UpdateSceneOrderList();
                lstSceneOrder.SelectedIndex = selectedIndex - 1;
            }
        }

        private void BtnMoveDown_Click(object sender, EventArgs e)
        {
            int selectedIndex = lstSceneOrder.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < currentSceneOrder.Count - 1)
            {
                string scene = currentSceneOrder[selectedIndex];
                
                // Empêcher de déplacer Menu
                if (scene == "Menu")
                {
                    MessageBox.Show("La scène Menu doit rester en première position !", 
                                "Action interdite", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Warning);
                    return;
                }
                
                // Code existant pour déplacer vers le bas
                currentSceneOrder.RemoveAt(selectedIndex);
                currentSceneOrder.Insert(selectedIndex + 1, scene);
                UpdateSceneOrderList();
                lstSceneOrder.SelectedIndex = selectedIndex + 1;
            }
        }

        private void BtnClearOrder_Click(object sender, EventArgs e)
        {
            // Vider la liste mais garder Menu
            currentSceneOrder.Clear();
            currentSceneOrder.Add("Menu");
            UpdateSceneOrderList();
            LogMessage("🗑️ Séquence vidée (Menu conservé)", Color.FromArgb(220, 53, 69));
        }

        private void AcceptClients()
        {
            while (isRunning)
            {
                try
                {
                    if (server != null && server.Server.IsBound)
                    {
                        TcpClient client = server.AcceptTcpClient();
                        lock (connectedClients)
                        {
                            connectedClients.Add(client);
                        }

                        string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                        LogMessage($"🔌 Client connecté: {clientIP}", Color.FromArgb(40, 167, 69));

                        this.Invoke(new Action(() =>
                        {
                            lblClientsConnected.Text = connectedClients.Count.ToString();
                            UpdateGameControls();
                            BtnStartGame_Click(null, EventArgs.Empty);
                        }));

                        Thread clientThread = new Thread(() => HandleClient(client));
                        clientThread.IsBackground = true;
                        clientThread.Start();
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException ex)
                {
                    if (isRunning)
                        LogMessage($"🔴 Erreur socket: {ex.Message}", Color.FromArgb(220, 53, 69));
                    break;
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        LogMessage($"🔴 Erreur acceptation: {ex.Message}", Color.FromArgb(220, 53, 69));
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
                byte[] buffer = new byte[1024];

                while (isRunning)
                {
                    if (client.Client.Poll(0, SelectMode.SelectRead) && client.Client.Available == 0)
                    {
                        break;
                    }
                    if (stream.DataAvailable)
                    {
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            HandleClientMessage(message, client);
                            UpdateGameStatus();
                        }
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                LogMessage($"🔴 Erreur communication {clientIP}: {e.Message}", Color.FromArgb(220, 53, 69));
            }
            finally
            {
                lock (connectedClients)
                {
                    if (connectedClients.Contains(client))
                    {
                        connectedClients.Remove(client);
                    }
                }

                this.Invoke(new Action(() =>
                {
                    lblClientsConnected.Text = connectedClients.Count.ToString();
                    LogMessage($"🔌 Client déconnecté: {clientIP}", Color.FromArgb(255, 193, 7));
                    UpdateGameControls();
                }));

                try { client.Close(); } catch { }
            }
        }

        private void HandleClientMessage(string message, TcpClient client)
        {
            string[] parts = message.Split(':');
            if (parts.Length == 0) return;

            switch (parts[0])
            {
                case "SCENE_LOADED":
                    if (parts.Length >= 4)
                    {
                        string sceneName = parts[1];
                        if (int.TryParse(parts[2], out int sceneIndex) && int.TryParse(parts[3], out int totalScenes))
                        {
                            currentSceneIndex = sceneIndex - 1;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void SendToAllClients(string message)
        {
            if (!isRunning) return;

            byte[] data = Encoding.UTF8.GetBytes(message);

            lock (connectedClients)
            {
                for (int i = connectedClients.Count - 1; i >= 0; i--)
                {
                    TcpClient client = connectedClients[i];
                    try
                    {
                        if (client.Connected)
                        {
                            NetworkStream stream = client.GetStream();
                            stream.Write(data, 0, data.Length);
                        }
                        else
                        {
                            connectedClients.RemoveAt(i);
                        }
                    }
                    catch (Exception e)
                    {
                        LogMessage($"🔴 Erreur envoi: {e.Message}", Color.FromArgb(220, 53, 69));
                        connectedClients.RemoveAt(i);
                    }
                }
            }

            this.Invoke(new Action(() =>
            {
                lblClientsConnected.Text = connectedClients.Count.ToString();
            }));
        }

        private void UpdateSceneOrderList()
        {
            lstSceneOrder.Items.Clear();
            for (int i = 0; i < currentSceneOrder.Count; i++)
            {
                lstSceneOrder.Items.Add($"{i + 1}. {currentSceneOrder[i]}");
            }
            UpdateGameStatus();
        }

        private void UpdateGameStatus()
        {
            if (currentSceneOrder.Count > 0)
            {
                if (currentSceneIndex < currentSceneOrder.Count)
                {
                    string currentScene = currentSceneOrder[currentSceneIndex];
                    lblCurrentScene.Text = $"🎯 {currentScene} ({currentSceneIndex + 1}/{currentSceneOrder.Count})";

                    progressGame.Maximum = currentSceneOrder.Count;
                    progressGame.Value = currentSceneIndex + 1;

                    // Mise à jour du pourcentage dans la card
                    int percentage = (int)((float)(currentSceneIndex + 1) / currentSceneOrder.Count * 100);
                    ((Label)progressCard.Controls[1]).Text = $"{percentage}%";
                }
                else
                {
                    lblCurrentScene.Text = "🏁 JEU TERMINÉ";
                    progressGame.Value = progressGame.Maximum;

                    ((Label)progressCard.Controls[1]).Text = "100%";
                }
            }
            else
            {
                lblCurrentScene.Text = "⏸️ En attente de configuration...";
                progressGame.Value = 0;

                ((Label)progressCard.Controls[1]).Text = "0%";
            }

            // Mise à jour du nombre de scènes
            ((Label)sceneCountCard.Controls[1]).Text = currentSceneOrder.Count.ToString();

            UpdateGameControls();
        }

        private void UpdateGameControls()
        {
            bool serverRunning = isRunning;
            bool clientsConnected = connectedClients.Count > 0;
            bool scenesConfigured = currentSceneOrder.Count > 0;
            bool hasNextScene = currentSceneIndex < currentSceneOrder.Count - 1;

            btnStartGame.Enabled = serverRunning && clientsConnected && scenesConfigured;
            btnNextScene.Enabled = serverRunning && clientsConnected && scenesConfigured;
            btnResetGame.Enabled = serverRunning && clientsConnected;

            if (scenesConfigured && hasNextScene)
            {
                btnNextScene.Text = "⏭️ SUIVANT";
            }
            else if (scenesConfigured)
            {
                btnNextScene.Text = "🏁 FINIR";
            }
        }

        private void UpdateUI()
        {
            UpdateSceneOrderList();
            UpdateGameControls();
        }

        private void LogMessage(string message, Color color)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => LogMessage(message, color)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = Color.FromArgb(108, 117, 125);
            txtLog.AppendText($"[{timestamp}] ");
            txtLog.SelectionColor = color;
            txtLog.AppendText($"{message}\n");
            txtLog.ScrollToCaret();
        }

        private void StopServer()
        {
            try
            {
                isRunning = false;

                lock (connectedClients)
                {
                    foreach (TcpClient client in connectedClients)
                    {
                        try
                        {
                            client.Close();
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"⚠️ Erreur fermeture: {ex.Message}", Color.FromArgb(255, 193, 7));
                        }
                    }
                    connectedClients.Clear();
                }

                if (server != null)
                {
                    server.Stop();
                    server = null;
                }

                if (serverThread != null && serverThread.IsAlive)
                {
                    if (!serverThread.Join(5000))
                    {
                        serverThread.Abort();
                    }
                    serverThread = null;
                }

                lblClientsConnected.Text = "0";
                UpdateGameControls();

                LogMessage("🛑 Serveur arrêté", Color.FromArgb(220, 53, 69));
            }
            catch (Exception ex)
            {
                LogMessage($"🔴 Erreur arrêt serveur: {ex.Message}", Color.FromArgb(220, 53, 69));
                MessageBox.Show($"Erreur lors de l'arrêt du serveur: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (isRunning)
            {
                DialogResult result = MessageBox.Show(
                    "Le serveur est encore en fonctionnement. Voulez-vous l'arrêter et fermer l'application ?",
                    "Confirmation de fermeture",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    StopServer();
                }
                else
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void LoadDefaultSceneOrder()
        {
            try
            {
                if (File.Exists("DefaultSceneOrder.txt"))
                {
                    string[] lines = File.ReadAllLines("DefaultSceneOrder.txt");
                    currentSceneOrder.Clear(); // Vider d'abord la liste par défaut
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            currentSceneOrder.Add(trimmed);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement de la scène par défaut : " + ex.Message);
            }
        }
    }

    // Classe pour les appels Windows API (déplacement de fenêtre)
    public static class User32
    {
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
    }
}