using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

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
            "MainMenu", "ChooseCharacter", "Platformer", "Lock",
            "Labo+Dragon", "Animation1", "Donjon", "Stage 1",
            "Stage 2", "Animation-1", "Animation2", "Animation2-2"
        };

        private List<string> currentSceneOrder = new List<string>
        {
            "MainMenu", "ChooseCharacter", "Platformer", "Lock",
            "Labo+Dragon", "Animation1", "Stage 1",
            "Stage 2", "Animation-1", "Animation2", "Animation2-2"
        };
        private int currentSceneIndex = 0;

        // Contrôles UI
        // private Button btnStartServer;
        // private Button btnStopServer;
        private Button btnStartGame;
        private Button btnNextScene;
        private Button btnResetGame;
        private Button btnSaveOrder;
        // private Button btnLoadOrder;
        // private Label lblServerStatus;
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
        // private ComboBox cmbGoToScene;
        // private Button btnGoToScene;
        private ProgressBar progressGame;

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

            // Configuration de la fenêtre principale
            this.Text = "Contrôle Escape Game - Interface Maître";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 240);

            // Panel serveur (en haut)
            // Panel panelServer = new Panel();
            // panelServer.Location = new Point(10, 10);
            // panelServer.Size = new Size(960, 80);
            // panelServer.BorderStyle = BorderStyle.FixedSingle;
            // panelServer.BackColor = Color.White;

            // Label lblServerTitle = new Label();
            // lblServerTitle.Text = "SERVEUR DE CONTRÔLE";
            // lblServerTitle.Font = new Font("Arial", 12, FontStyle.Bold);
            // lblServerTitle.Location = new Point(10, 10);
            // lblServerTitle.Size = new Size(200, 25);

            // btnStartServer = new Button();
            // btnStartServer.Text = "▶ DÉMARRER SERVEUR";
            // btnStartServer.Location = new Point(10, 40);
            // btnStartServer.Size = new Size(150, 30);
            // btnStartServer.BackColor = Color.FromArgb(76, 175, 80);
            // btnStartServer.ForeColor = Color.White;
            // btnStartServer.FlatStyle = FlatStyle.Flat;
            // btnStartServer.Font = new Font("Arial", 9, FontStyle.Bold);
            // btnStartServer.Click += BtnStartServer_Click;

            // btnStopServer = new Button();
            // btnStopServer.Text = "⏹ ARRÊTER SERVEUR";
            // btnStopServer.Location = new Point(170, 40);
            // btnStopServer.Size = new Size(150, 30);
            // btnStopServer.BackColor = Color.FromArgb(244, 67, 54);
            // btnStopServer.ForeColor = Color.White;
            // btnStopServer.FlatStyle = FlatStyle.Flat;
            // btnStopServer.Font = new Font("Arial", 9, FontStyle.Bold);
            // btnStopServer.Click += BtnStopServer_Click;
            // btnStopServer.Enabled = false;

            // lblServerStatus = new Label();
            // lblServerStatus.Text = "Statut: Arrêté";
            // lblServerStatus.Location = new Point(340, 45);
            // lblServerStatus.Size = new Size(150, 20);
            // lblServerStatus.Font = new Font("Arial", 9, FontStyle.Bold);
            // lblServerStatus.ForeColor = Color.Red;

            lblClientsConnected = new Label();
            lblClientsConnected.Text = "Clients connectés: 0";
            lblClientsConnected.Location = new Point(10, 10);
            lblClientsConnected.Size = new Size(150, 20);
            lblClientsConnected.Font = new Font("Arial", 9);

            // panelServer.Controls.AddRange(new Control[] { lblServerTitle, btnStartServer, btnStopServer, lblServerStatus, lblClientsConnected });
            // panelServer.Controls.AddRange(new Control[] { lblClientsConnected });

            // Panel configuration des scènes (gauche)
            Panel panelScenes = new Panel();
            panelScenes.Location = new Point(10, 100);
            panelScenes.Size = new Size(470, 350);
            panelScenes.BorderStyle = BorderStyle.FixedSingle;
            panelScenes.BackColor = Color.White;

            Label lblScenesTitle = new Label();
            lblScenesTitle.Text = "CONFIGURATION DES SCÈNES";
            lblScenesTitle.Font = new Font("Arial", 11, FontStyle.Bold);
            lblScenesTitle.Location = new Point(10, 10);
            lblScenesTitle.Size = new Size(250, 25);

            Label lblAvailable = new Label();
            lblAvailable.Text = "Scènes disponibles:";
            lblAvailable.Location = new Point(10, 40);
            lblAvailable.Size = new Size(120, 20);
            lblAvailable.Font = new Font("Arial", 9);

            lstAvailableScenes = new ListBox();
            lstAvailableScenes.Location = new Point(10, 60);
            lstAvailableScenes.Size = new Size(180, 200);
            lstAvailableScenes.Font = new Font("Arial", 9);
            lstAvailableScenes.Items.AddRange(availableScenes.ToArray());

            Label lblOrder = new Label();
            lblOrder.Text = "Ordre du jeu:";
            lblOrder.Location = new Point(280, 40);
            lblOrder.Size = new Size(100, 20);
            lblOrder.Font = new Font("Arial", 9);

            lstSceneOrder = new ListBox();
            lstSceneOrder.Location = new Point(280, 60);
            lstSceneOrder.Size = new Size(180, 200);
            lstSceneOrder.Font = new Font("Arial", 9);

            // Boutons de gestion des scènes
            btnAddScene = new Button();
            btnAddScene.Text = "➤";
            btnAddScene.Location = new Point(200, 100);
            btnAddScene.Size = new Size(70, 30);
            btnAddScene.Font = new Font("Arial", 12, FontStyle.Bold);
            btnAddScene.Click += BtnAddScene_Click;

            btnRemoveScene = new Button();
            btnRemoveScene.Text = "✕";
            btnRemoveScene.Location = new Point(200, 140);
            btnRemoveScene.Size = new Size(70, 30);
            btnRemoveScene.Font = new Font("Arial", 12, FontStyle.Bold);
            btnRemoveScene.Click += BtnRemoveScene_Click;

            btnMoveUp = new Button();
            btnMoveUp.Text = "▲";
            btnMoveUp.Location = new Point(200, 180);
            btnMoveUp.Size = new Size(70, 30);
            btnMoveUp.Font = new Font("Arial", 12, FontStyle.Bold);
            btnMoveUp.Click += BtnMoveUp_Click;

            btnMoveDown = new Button();
            btnMoveDown.Text = "▼";
            btnMoveDown.Location = new Point(200, 220);
            btnMoveDown.Size = new Size(70, 30);
            btnMoveDown.Font = new Font("Arial", 12, FontStyle.Bold);
            btnMoveDown.Click += BtnMoveDown_Click;

            btnClearOrder = new Button();
            btnClearOrder.Text = "Vider";
            btnClearOrder.Location = new Point(280, 270);
            btnClearOrder.Size = new Size(80, 25);
            btnClearOrder.Click += BtnClearOrder_Click;

            btnSaveOrder = new Button();
            btnSaveOrder.Text = "Sauvegarder";
            btnSaveOrder.Location = new Point(280, 300);
            btnSaveOrder.Size = new Size(80, 25);
            btnSaveOrder.Click += BtnSaveOrder_Click;

            // btnLoadOrder = new Button();
            // btnLoadOrder.Text = "Charger";
            // btnLoadOrder.Location = new Point(380, 300);
            // btnLoadOrder.Size = new Size(80, 25);
            // btnLoadOrder.Click += BtnLoadOrder_Click;

            // panelScenes.Controls.AddRange(new Control[] {
            //     lblScenesTitle, lblAvailable, lstAvailableScenes, lblOrder, lstSceneOrder,
            //     btnAddScene, btnRemoveScene, btnMoveUp, btnMoveDown, btnClearOrder, btnSaveOrder, btnLoadOrder
            // });
            panelScenes.Controls.AddRange(new Control[] {
                lblScenesTitle, lblAvailable, lstAvailableScenes, lblOrder, lstSceneOrder,
                btnAddScene, btnRemoveScene, btnMoveUp, btnMoveDown, btnClearOrder, btnSaveOrder
            });

            // Panel contrôle du jeu (droite)
            Panel panelControl = new Panel();
            panelControl.Location = new Point(500, 100);
            panelControl.Size = new Size(470, 350);
            panelControl.BorderStyle = BorderStyle.FixedSingle;
            panelControl.BackColor = Color.White;

            Label lblControlTitle = new Label();
            lblControlTitle.Text = "CONTRÔLE DU JEU";
            lblControlTitle.Font = new Font("Arial", 11, FontStyle.Bold);
            lblControlTitle.Location = new Point(10, 10);
            lblControlTitle.Size = new Size(200, 25);

            btnStartGame = new Button();
            btnStartGame.Text = "🎮 DÉMARRER LE JEU";
            btnStartGame.Location = new Point(10, 50);
            btnStartGame.Size = new Size(200, 40);
            btnStartGame.BackColor = Color.FromArgb(76, 175, 80);
            btnStartGame.ForeColor = Color.White;
            btnStartGame.FlatStyle = FlatStyle.Flat;
            btnStartGame.Font = new Font("Arial", 11, FontStyle.Bold);
            btnStartGame.Click += BtnStartGame_Click;

            btnNextScene = new Button();
            btnNextScene.Text = "⏭ SCÈNE SUIVANTE";
            btnNextScene.Location = new Point(250, 50);
            btnNextScene.Size = new Size(150, 40);
            btnNextScene.BackColor = Color.FromArgb(33, 150, 243);
            btnNextScene.ForeColor = Color.White;
            btnNextScene.FlatStyle = FlatStyle.Flat;
            btnNextScene.Font = new Font("Arial", 10, FontStyle.Bold);
            btnNextScene.Click += BtnNextScene_Click;

            btnResetGame = new Button();
            btnResetGame.Text = "🔄 RÉINITIALISER";
            btnResetGame.Location = new Point(10, 100);
            btnResetGame.Size = new Size(150, 30);
            btnResetGame.BackColor = Color.FromArgb(255, 152, 0);
            btnResetGame.ForeColor = Color.White;
            btnResetGame.FlatStyle = FlatStyle.Flat;
            btnResetGame.Font = new Font("Arial", 9, FontStyle.Bold);
            btnResetGame.Click += BtnResetGame_Click;

            // // Aller à une scène spécifique
            // Label lblGoTo = new Label();
            // lblGoTo.Text = "Aller à la scène:";
            // lblGoTo.Location = new Point(10, 150);
            // lblGoTo.Size = new Size(100, 20);

            // cmbGoToScene = new ComboBox();
            // cmbGoToScene.Location = new Point(10, 170);
            // cmbGoToScene.Size = new Size(150, 25);
            // cmbGoToScene.DropDownStyle = ComboBoxStyle.DropDownList;

            // btnGoToScene = new Button();
            // btnGoToScene.Text = "Aller";
            // btnGoToScene.Location = new Point(170, 170);
            // btnGoToScene.Size = new Size(60, 25);
            // btnGoToScene.Click += BtnGoToScene_Click;

            // Statut du jeu
            lblCurrentScene = new Label();
            lblCurrentScene.Text = "Scène actuelle: Aucune";
            lblCurrentScene.Location = new Point(10, 210);
            lblCurrentScene.Size = new Size(200, 20);
            lblCurrentScene.Font = new Font("Arial", 9, FontStyle.Bold);

            Label lblProgress = new Label();
            lblProgress.Text = "Progression:";
            lblProgress.Location = new Point(10, 240);
            lblProgress.Size = new Size(80, 20);

            progressGame = new ProgressBar();
            progressGame.Location = new Point(90, 240);
            progressGame.Size = new Size(200, 20);
            progressGame.Style = ProgressBarStyle.Continuous;

            // panelControl.Controls.AddRange(new Control[] {
            //     lblControlTitle, btnStartGame, btnNextScene, btnResetGame,
            //     lblGoTo, cmbGoToScene, btnGoToScene, lblCurrentScene, lblProgress, progressGame
            // });

            panelControl.Controls.AddRange(new Control[] {
                lblControlTitle, btnStartGame, btnNextScene, btnResetGame, lblCurrentScene, lblProgress, progressGame
            });

            // Log des événements (bas)
            Panel panelLog = new Panel();
            panelLog.Location = new Point(10, 460);
            panelLog.Size = new Size(960, 180);
            panelLog.BorderStyle = BorderStyle.FixedSingle;
            panelLog.BackColor = Color.White;

            Label lblLogTitle = new Label();
            lblLogTitle.Text = "JOURNAL DES ÉVÉNEMENTS";
            lblLogTitle.Font = new Font("Arial", 11, FontStyle.Bold);
            lblLogTitle.Location = new Point(10, 10);
            lblLogTitle.Size = new Size(200, 25);

            txtLog = new RichTextBox();
            txtLog.Location = new Point(10, 35);
            txtLog.Size = new Size(940, 135);
            txtLog.ReadOnly = true;
            txtLog.BackColor = Color.FromArgb(248, 248, 248);
            txtLog.Font = new Font("Consolas", 8);

            panelLog.Controls.AddRange(new Control[] { lblLogTitle, txtLog });

            // Ajouter tous les panels à la forme
            this.Controls.AddRange(new Control[] { lblClientsConnected, panelScenes, panelControl, panelLog });

            this.ResumeLayout(false);
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

                // btnStartServer.Enabled = false;
                // btnStopServer.Enabled = true;
                // lblServerStatus.Text = "Statut: Démarré";
                // lblServerStatus.ForeColor = Color.Green;

                LogMessage("✅ Serveur démarré sur le port 12345", Color.Green);
                UpdateGameControls();
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Erreur serveur: {ex.Message}", Color.Red);
                MessageBox.Show($"Erreur lors du démarrage du serveur: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // private void BtnStartServer_Click(object sender, EventArgs e)
        // {
        //     StartServer();
        // }

        // private void BtnStopServer_Click(object sender, EventArgs e)
        // {
        //     StopServer();
        // }
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
            //SendToAllClients("START_GAME");

            LogMessage($"🎮 Jeu démarré avec {currentSceneOrder.Count} scènes", Color.Blue);
            UpdateGameStatus();
        }

        private void BtnNextScene_Click(object sender, EventArgs e)
        {
            if (currentSceneIndex < currentSceneOrder.Count - 1)
            {
                SendToAllClients("NEXT_SCENE");
                currentSceneIndex++;
                LogMessage($"⏭ Passage à la scène suivante: {currentSceneOrder[currentSceneIndex]}", Color.Blue);
            }
            else
            {
                // On est à la dernière scène
                LogMessage("⚠️ Déjà à la dernière scène du jeu", Color.Orange);
                if (MessageBox.Show("Vous êtes à la dernière scène. Voulez-vous terminer le jeu ?", 
                                "Fin du jeu", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    SendToAllClients("END_GAME");
                    LogMessage("🏁 Jeu terminé", Color.Purple);
                }
            }
            UpdateGameStatus();
        }


        private void BtnResetGame_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Êtes-vous sûr de vouloir réinitialiser le jeu ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SendToAllClients("RESET_GAME");
                currentSceneIndex = 0;
                LogMessage("🔄 Jeu réinitialisé", Color.Orange);
                UpdateGameStatus();
            }
        }

        // private void BtnGoToScene_Click(object sender, EventArgs e)
        // {
        //     if (cmbGoToScene.SelectedItem != null)
        //     {
        //         string sceneName = cmbGoToScene.SelectedItem.ToString();
        //         SendToAllClients($"GOTO_SCENE:{sceneName}");

        //         int sceneIndex = currentSceneOrder.IndexOf(sceneName);
        //         if (sceneIndex >= 0)
        //         {
        //             currentSceneIndex = sceneIndex;
        //         }

        //         LogMessage($"🎯 Saut vers la scène: {sceneName}", Color.Purple);
        //         UpdateGameStatus();
        //     }
        // }

        private void BtnAddScene_Click(object sender, EventArgs e)
        {
            if (lstAvailableScenes.SelectedItem != null)
            {
                string selectedScene = lstAvailableScenes.SelectedItem.ToString();
                currentSceneOrder.Add(selectedScene);
                UpdateSceneOrderList();
                LogMessage($"➕ Scène ajoutée: {selectedScene}", Color.DarkGreen);
            }
        }

        private void BtnRemoveScene_Click(object sender, EventArgs e)
        {
            if (lstSceneOrder.SelectedIndex >= 0)
            {
                string removedScene = currentSceneOrder[lstSceneOrder.SelectedIndex];
                currentSceneOrder.RemoveAt(lstSceneOrder.SelectedIndex);
                UpdateSceneOrderList();
                LogMessage($"➖ Scène supprimée: {removedScene}", Color.DarkRed);
            }
        }

        private void BtnMoveUp_Click(object sender, EventArgs e)
        {
            int selectedIndex = lstSceneOrder.SelectedIndex;
            if (selectedIndex > 0)
            {
                string scene = currentSceneOrder[selectedIndex];
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
                currentSceneOrder.RemoveAt(selectedIndex);
                currentSceneOrder.Insert(selectedIndex + 1, scene);
                UpdateSceneOrderList();
                lstSceneOrder.SelectedIndex = selectedIndex + 1;
            }
        }

        private void BtnClearOrder_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Vider complètement l'ordre des scènes ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                currentSceneOrder.Clear();
                UpdateSceneOrderList();
                LogMessage("🗑 Ordre des scènes vidé", Color.Red);
            }
        }

        private void BtnSaveOrder_Click(object sender, EventArgs e)
        {
            if (currentSceneOrder.Count == 0)
            {
                MessageBox.Show("Aucun ordre à sauvegarder !", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Fichiers texte (*.txt)|*.txt";
            saveDialog.DefaultExt = "txt";
            saveDialog.FileName = "SceneOrder";

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllLines(saveDialog.FileName, currentSceneOrder);
                    LogMessage($"💾 Ordre sauvegardé: {Path.GetFileName(saveDialog.FileName)}", Color.DarkBlue);
                    MessageBox.Show("Ordre sauvegardé avec succès !", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    LogMessage($"❌ Erreur sauvegarde: {ex.Message}", Color.Red);
                    MessageBox.Show($"Erreur lors de la sauvegarde: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // private void BtnLoadOrder_Click(object sender, EventArgs e)
        // {
        //     OpenFileDialog openDialog = new OpenFileDialog();
        //     openDialog.Filter = "Fichiers texte (*.txt)|*.txt";
        //     openDialog.DefaultExt = "txt";

        //     if (openDialog.ShowDialog() == DialogResult.OK)
        //     {
        //         try
        //         {
        //             string[] lines = File.ReadAllLines(openDialog.FileName);
        //             currentSceneOrder.Clear();

        //             foreach (string line in lines)
        //             {
        //                 string trimmed = line.Trim();
        //                 if (!string.IsNullOrEmpty(trimmed))
        //                 {
        //                     currentSceneOrder.Add(trimmed);
        //                 }
        //             }

        //             UpdateSceneOrderList();
        //             LogMessage($"📂 Ordre chargé: {Path.GetFileName(openDialog.FileName)} ({currentSceneOrder.Count} scènes)", Color.DarkBlue);
        //             MessageBox.Show($"Ordre chargé avec succès ! ({currentSceneOrder.Count} scènes)", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //         }
        //         catch (Exception ex)
        //         {
        //             LogMessage($"❌ Erreur chargement: {ex.Message}", Color.Red);
        //             MessageBox.Show($"Erreur lors du chargement: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //         }
        //     }
        // }

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
                        LogMessage($"🔌 Nouveau client connecté: {clientIP}", Color.Green);

                        this.Invoke(new Action(() => {
                            lblClientsConnected.Text = $"Clients connectés: {connectedClients.Count}";
                            UpdateGameControls();
                        }));

                        Thread clientThread = new Thread(() => HandleClient(client));
                        clientThread.IsBackground = true; // Thread en arrière-plan
                        clientThread.Start();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Le serveur a été fermé, c'est normal
                    break;
                }
                catch (SocketException ex)
                {
                    if (isRunning) // Ne logger que si on n'est pas en train d'arrêter
                        LogMessage($"❌ Erreur socket: {ex.Message}", Color.Red);
                    break;
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        LogMessage($"❌ Erreur acceptation client: {ex.Message}", Color.Red);
                }
            }
        }
        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (client.Connected && isRunning)
            {
                try
                {
                    if (stream.DataAvailable)
                    {
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            LogMessage($"📨 Message reçu: {message}", Color.DarkGray);
                        }
                    }
                    Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    LogMessage($"❌ Erreur communication: {e.Message}", Color.Red);
                    break;
                }
            }

            lock (connectedClients)
            {
                connectedClients.Remove(client);
            }

            this.Invoke(new Action(() => {
                lblClientsConnected.Text = $"Clients connectés: {connectedClients.Count}";
                LogMessage("🔌 Client déconnecté", Color.Orange);
                UpdateGameControls();
            }));
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
                        LogMessage($"❌ Erreur envoi: {e.Message}", Color.Red);
                        connectedClients.RemoveAt(i);
                    }
                }
            }

            this.Invoke(new Action(() => {
                lblClientsConnected.Text = $"Clients connectés: {connectedClients.Count}";
            }));
        }

        private void UpdateSceneOrderList()
        {
            lstSceneOrder.Items.Clear();
            for (int i = 0; i < currentSceneOrder.Count; i++)
            {
                lstSceneOrder.Items.Add($"{i + 1}. {currentSceneOrder[i]}");
            }

            // cmbGoToScene.Items.Clear();
            // cmbGoToScene.Items.AddRange(currentSceneOrder.ToArray());

            UpdateGameStatus();
        }

        private void UpdateGameStatus()
        {
            if (currentSceneOrder.Count > 0)
            {
                if (currentSceneIndex < currentSceneOrder.Count)
                {
                    string currentScene = currentSceneOrder[currentSceneIndex];
                    lblCurrentScene.Text = $"Scène actuelle: {currentScene} ({currentSceneIndex + 1}/{currentSceneOrder.Count})";
                    
                    // Mise à jour de la barre de progression
                    progressGame.Maximum = currentSceneOrder.Count;
                    progressGame.Value = currentSceneIndex + 1;
                    
                    // Vérifier si c'est la dernière scène
                    if (currentSceneIndex == currentSceneOrder.Count - 1)
                    {
                        lblCurrentScene.Text += " (DERNIÈRE SCÈNE)";
                        lblCurrentScene.ForeColor = Color.Red;
                    }
                    else
                    {
                        lblCurrentScene.ForeColor = Color.Black;
                    }
                }
                else
                {
                    // Cette condition ne devrait plus jamais être atteinte avec la logique corrigée
                    lblCurrentScene.Text = "Jeu terminé";
                    lblCurrentScene.ForeColor = Color.Green;
                    progressGame.Value = progressGame.Maximum;
                }
            }
            else
            {
                lblCurrentScene.Text = "Scène actuelle: Aucune";
                lblCurrentScene.ForeColor = Color.Black;
                progressGame.Value = 0;
            }
            
            // Mettre à jour l'état des boutons
            UpdateGameControls();
        }

        private void UpdateGameControls()
        {
            bool serverRunning = isRunning;
            bool clientsConnected = connectedClients.Count > 0;
            bool scenesConfigured = currentSceneOrder.Count > 0;
            bool hasNextScene = currentSceneIndex < currentSceneOrder.Count - 1;

            btnStartGame.Enabled = serverRunning && clientsConnected && scenesConfigured;
            btnNextScene.Enabled = serverRunning && clientsConnected && scenesConfigured; // Toujours activé pour permettre la fin
            btnResetGame.Enabled = serverRunning && clientsConnected;
            // btnGoToScene.Enabled = serverRunning && clientsConnected && scenesConfigured;
            
            // Changer le texte du bouton selon le contexte
            if (scenesConfigured && hasNextScene)
            {
                btnNextScene.Text = "⏭ SCÈNE SUIVANTE";
                btnNextScene.BackColor = Color.FromArgb(33, 150, 243);
            }
            else if (scenesConfigured && !hasNextScene)
            {
                btnNextScene.Text = "🏁 TERMINER JEU";
                btnNextScene.BackColor = Color.FromArgb(156, 39, 176); // Violet pour indiquer la fin
            }
        }

        private void GoToLastScene()
        {
            if (currentSceneOrder.Count > 0)
            {
                currentSceneIndex = currentSceneOrder.Count - 1;
                string lastScene = currentSceneOrder[currentSceneIndex];
                SendToAllClients($"GOTO_SCENE:{lastScene}");
                LogMessage($"🎯 Saut vers la dernière scène: {lastScene}", Color.Purple);
                UpdateGameStatus();
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
            txtLog.SelectionColor = Color.Gray;
            txtLog.AppendText($"[{timestamp}] ");
            txtLog.SelectionColor = color;
            txtLog.AppendText($"{message}\n");
            txtLog.ScrollToCaret();
        }
        private void StopServer()
        {
            try
            {
                // Arrêter la boucle d'acceptation des clients
                isRunning = false;

                // Fermer toutes les connexions clients
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
                            LogMessage($"⚠️ Erreur fermeture client: {ex.Message}", Color.Orange);
                        }
                    }
                    connectedClients.Clear();
                }

                // Arrêter le serveur TCP
                if (server != null)
                {
                    server.Stop();
                    server = null;
                }

                // Attendre que le thread serveur se termine
                if (serverThread != null && serverThread.IsAlive)
                {
                    if (!serverThread.Join(5000)) // Attendre 5 secondes maximum
                    {
                        serverThread.Abort(); // Force l'arrêt si nécessaire
                    }
                    serverThread = null;
                }

                // Mettre à jour l'interface utilisateur
                // btnStartServer.Enabled = true;
                // btnStopServer.Enabled = false;
                // lblServerStatus.Text = "Statut: Arrêté";
                // lblServerStatus.ForeColor = Color.Red;
                lblClientsConnected.Text = "Clients connectés: 0";

                // Désactiver les contrôles de jeu
                UpdateGameControls();

                LogMessage("🛑 Serveur arrêté avec succès", Color.Red);
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Erreur lors de l'arrêt du serveur: {ex.Message}", Color.Red);
                MessageBox.Show($"Erreur lors de l'arrêt du serveur: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Ajoutez cette méthode pour gérer la fermeture du formulaire
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
                    e.Cancel = true; // Annuler la fermeture
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
}
