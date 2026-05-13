using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SoulReaverEditor
{
    internal sealed class MainForm : Form
    {
        private readonly string _startupPath;

        private DiscImage _disc;
        private IsoFileEntry _isoRoot;
        private IsoFileEntry _bigIsoEntry;
        private Stream _bigFileStream;
        private BigFileReader _bigFile;

        private TreeView _tree;
        private ListView _list;
        private TextBox _details;
        private TextBox _hex;
        private TextBox _strings;
        private TextBox _searchText;
        private ComboBox _searchMode;
        private ComboBox _searchScope;
        private ListView _searchResults;
        private ListView _objectCandidates;
        private ListView _terrainCandidates;
        private ListView _paletteCandidates;
        private ListView _audioCandidates;
        private ComboBox _levelSelector;
        private LevelCanvas _levelCanvas;
        private ListView _levelObjects;
        private ListView _levelPortals;
        private TextBox _levelSummary;
        private NumericUpDown _objX;
        private NumericUpDown _objY;
        private NumericUpDown _objZ;
        private NumericUpDown _rotX;
        private NumericUpDown _rotY;
        private NumericUpDown _rotZ;
        private Label _objectDelta;
        private CheckBox _showTerrain;
        private CheckBox _showObjects;
        private CheckBox _showPortals;
        private ComboBox _levelViewMode;
        private CheckBox _dragObjects;
        private SR1LevelDocument _currentLevel;
        private SR1LevelDocument _selectedLevelDocument;
        private readonly List<SR1LevelDocument> _loadedLevels = new List<SR1LevelDocument>();
        private SplitContainer _mainSplit;
        private SplitContainer _infoSplit;
        private SplitContainer _previewSplit;
        private ToolStripStatusLabel _status;
        private ToolStripProgressBar _progress;

        public MainForm(string startupPath)
        {
            _startupPath = startupPath;
            Text = "Legacy of Kain: Soul Reaver Editor";
            Width = 1260;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);

            BuildUi();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyInitialSplitterLayout();
            BeginInvoke(new MethodInvoker(OpenStartupImage));
        }

        private void OpenStartupImage()
        {
            if (!string.IsNullOrEmpty(_startupPath))
            {
                OpenImage(_startupPath);
                return;
            }

            string defaultCue = @"C:\Users\monty\OneDrive\Desktop\Legacy of Kain - Soul Reaver (USA).cue";
            if (File.Exists(defaultCue))
            {
                OpenImage(defaultCue);
            }
        }

        private void ApplyInitialSplitterLayout()
        {
            SetSplitterDistanceSafe(_mainSplit, 360);
            SetSplitterDistanceSafe(_infoSplit, 270);
            SetSplitterDistanceSafe(_previewSplit, 410);
        }

        private static void SetSplitterDistanceSafe(SplitContainer split, int preferred)
        {
            if (split == null) return;
            int available = split.Orientation == Orientation.Vertical ? split.ClientSize.Width : split.ClientSize.Height;
            int min = split.Panel1MinSize;
            int max = available - split.Panel2MinSize - split.SplitterWidth;
            if (max < min) return;
            int distance = Math.Max(min, Math.Min(preferred, max));
            split.SplitterDistance = distance;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            CloseImage();
            base.OnFormClosed(e);
        }

        private void BuildUi()
        {
            MainMenuStrip = BuildMenu();
            Controls.Add(MainMenuStrip);

            StatusStrip statusStrip = new StatusStrip();
            _status = new ToolStripStatusLabel("Ready");
            _progress = new ToolStripProgressBar();
            _progress.Visible = false;
            _progress.Minimum = 0;
            _progress.Maximum = 100;
            statusStrip.Items.Add(_status);
            statusStrip.Items.Add(_progress);
            Controls.Add(statusStrip);

            _mainSplit = new SplitContainer();
            _mainSplit.Dock = DockStyle.Fill;
            _mainSplit.Panel1MinSize = 80;
            _mainSplit.Panel2MinSize = 80;
            Controls.Add(_mainSplit);
            _mainSplit.BringToFront();

            _tree = new TreeView();
            _tree.Dock = DockStyle.Fill;
            _tree.HideSelection = false;
            _tree.AfterSelect += delegate { DisplaySelection(); };
            _mainSplit.Panel1.Controls.Add(_tree);

            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            _mainSplit.Panel2.Controls.Add(tabs);

            TabPage infoTab = new TabPage("Info");
            tabs.TabPages.Add(infoTab);
            _infoSplit = new SplitContainer();
            _infoSplit.Dock = DockStyle.Fill;
            _infoSplit.Orientation = Orientation.Horizontal;
            _infoSplit.Panel1MinSize = 60;
            _infoSplit.Panel2MinSize = 60;
            infoTab.Controls.Add(_infoSplit);

            _details = new TextBox();
            _details.Dock = DockStyle.Fill;
            _details.Multiline = true;
            _details.ReadOnly = true;
            _details.ScrollBars = ScrollBars.Both;
            _details.Font = new Font("Consolas", 9F);
            _infoSplit.Panel1.Controls.Add(_details);

            _list = CreateListView();
            _list.DoubleClick += delegate { SelectTreeNodeForListItem(); };
            _infoSplit.Panel2.Controls.Add(_list);

            TabPage previewTab = new TabPage("Preview");
            tabs.TabPages.Add(previewTab);
            _previewSplit = new SplitContainer();
            _previewSplit.Dock = DockStyle.Fill;
            _previewSplit.Orientation = Orientation.Horizontal;
            _previewSplit.Panel1MinSize = 60;
            _previewSplit.Panel2MinSize = 60;
            previewTab.Controls.Add(_previewSplit);

            _hex = new TextBox();
            _hex.Dock = DockStyle.Fill;
            _hex.Multiline = true;
            _hex.ReadOnly = true;
            _hex.ScrollBars = ScrollBars.Both;
            _hex.Font = new Font("Consolas", 9F);
            _previewSplit.Panel1.Controls.Add(_hex);

            _strings = new TextBox();
            _strings.Dock = DockStyle.Fill;
            _strings.Multiline = true;
            _strings.ReadOnly = true;
            _strings.ScrollBars = ScrollBars.Both;
            _strings.Font = new Font("Consolas", 9F);
            _previewSplit.Panel2.Controls.Add(_strings);

            TabPage searchTab = new TabPage("Search");
            tabs.TabPages.Add(searchTab);
            BuildSearchTab(searchTab);

            TabPage levelTab = new TabPage("Level Editor");
            tabs.TabPages.Add(levelTab);
            BuildLevelEditorTab(levelTab);

            TabPage editorTab = new TabPage("Research Editors");
            tabs.TabPages.Add(editorTab);
            BuildEditorTab(editorTab);
        }

        private MenuStrip BuildMenu()
        {
            MenuStrip menu = new MenuStrip();
            ToolStripMenuItem file = new ToolStripMenuItem("&File");
            menu.Items.Add(file);

            ToolStripMenuItem open = new ToolStripMenuItem("&Open BIN/CUE...");
            open.Click += delegate { OpenImageDialog(); };
            file.DropDownItems.Add(open);

            ToolStripMenuItem openDefault = new ToolStripMenuItem("Open Provided Soul Reaver Image");
            openDefault.Click += delegate
            {
                string defaultCue = @"C:\Users\monty\OneDrive\Desktop\Legacy of Kain - Soul Reaver (USA).cue";
                OpenImage(defaultCue);
            };
            file.DropDownItems.Add(openDefault);

            file.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem export = new ToolStripMenuItem("&Export Selected...");
            export.Click += delegate { ExportSelected(); };
            file.DropDownItems.Add(export);

            ToolStripMenuItem replaceCopy = new ToolStripMenuItem("Replace Selected In New BIN Copy...");
            replaceCopy.Click += delegate { ReplaceSelectedInNewCopy(); };
            file.DropDownItems.Add(replaceCopy);

            ToolStripMenuItem extractFolder = new ToolStripMenuItem("Extract Selected BIGFILE Folder...");
            extractFolder.Click += delegate { ExtractSelectedBigFolder(); };
            file.DropDownItems.Add(extractFolder);

            ToolStripMenuItem extractAll = new ToolStripMenuItem("Extract All BIGFILE Entries...");
            extractAll.Click += delegate { ExtractAllBigFile(); };
            file.DropDownItems.Add(extractAll);

            file.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem exit = new ToolStripMenuItem("E&xit");
            exit.Click += delegate { Close(); };
            file.DropDownItems.Add(exit);

            ToolStripMenuItem tools = new ToolStripMenuItem("&Tools");
            menu.Items.Add(tools);
            ToolStripMenuItem scan = new ToolStripMenuItem("Scan Selected For Signatures");
            scan.Click += delegate { ScanSelectedForSignatures(); };
            tools.DropDownItems.Add(scan);

            return menu;
        }

        private static ListView CreateListView()
        {
            ListView view = new ListView();
            view.Dock = DockStyle.Fill;
            view.View = View.Details;
            view.FullRowSelect = true;
            view.GridLines = true;
            view.Columns.Add("Name", 260);
            view.Columns.Add("Kind", 150);
            view.Columns.Add("Size", 95);
            view.Columns.Add("Offset/LBA", 130);
            view.Columns.Add("Notes", 360);
            return view;
        }

        private void BuildSearchTab(TabPage searchTab)
        {
            Panel top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = 42;
            searchTab.Controls.Add(top);

            Label queryLabel = new Label();
            queryLabel.Text = "Find";
            queryLabel.AutoSize = true;
            queryLabel.Left = 10;
            queryLabel.Top = 13;
            top.Controls.Add(queryLabel);

            _searchText = new TextBox();
            _searchText.Left = 45;
            _searchText.Top = 9;
            _searchText.Width = 290;
            top.Controls.Add(_searchText);

            _searchMode = new ComboBox();
            _searchMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _searchMode.Left = 345;
            _searchMode.Top = 8;
            _searchMode.Width = 95;
            _searchMode.Items.Add("Text");
            _searchMode.Items.Add("Hex");
            _searchMode.SelectedIndex = 0;
            top.Controls.Add(_searchMode);

            _searchScope = new ComboBox();
            _searchScope.DropDownStyle = ComboBoxStyle.DropDownList;
            _searchScope.Left = 450;
            _searchScope.Top = 8;
            _searchScope.Width = 155;
            _searchScope.Items.Add("Selected resource");
            _searchScope.Items.Add("All BIGFILE entries");
            _searchScope.SelectedIndex = 0;
            top.Controls.Add(_searchScope);

            Button go = new Button();
            go.Text = "Search";
            go.Left = 615;
            go.Top = 7;
            go.Width = 90;
            go.Click += delegate { RunSearch(); };
            top.Controls.Add(go);

            Button signatures = new Button();
            signatures.Text = "Signatures";
            signatures.Left = 715;
            signatures.Top = 7;
            signatures.Width = 100;
            signatures.Click += delegate { ScanSelectedForSignatures(); };
            top.Controls.Add(signatures);

            _searchResults = new ListView();
            _searchResults.Dock = DockStyle.Fill;
            _searchResults.View = View.Details;
            _searchResults.FullRowSelect = true;
            _searchResults.GridLines = true;
            _searchResults.Columns.Add("Location", 360);
            _searchResults.Columns.Add("Offset", 120);
            _searchResults.Columns.Add("Description", 500);
            searchTab.Controls.Add(_searchResults);
            _searchResults.BringToFront();
        }

        private void BuildLevelEditorTab(TabPage levelTab)
        {
            Panel top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = 46;
            levelTab.Controls.Add(top);

            Label label = new Label();
            label.Text = "Room";
            label.AutoSize = true;
            label.Left = 10;
            label.Top = 15;
            top.Controls.Add(label);

            _levelSelector = new ComboBox();
            _levelSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            _levelSelector.Left = 55;
            _levelSelector.Top = 10;
            _levelSelector.Width = 300;
            top.Controls.Add(_levelSelector);

            Button load = new Button();
            load.Text = "Load";
            load.Left = 365;
            load.Top = 9;
            load.Width = 60;
            load.Click += delegate { LoadSelectedLevelFromCombo(); };
            top.Controls.Add(load);

            Button loadTree = new Button();
            loadTree.Text = "Load Entry";
            loadTree.Left = 435;
            loadTree.Top = 9;
            loadTree.Width = 85;
            loadTree.Click += delegate { LoadLevelFromTreeSelection(); };
            top.Controls.Add(loadTree);

            Button loadLinked = new Button();
            loadLinked.Text = "Linked";
            loadLinked.Left = 530;
            loadLinked.Top = 9;
            loadLinked.Width = 80;
            loadLinked.Click += delegate { LoadLinkedRooms(); };
            top.Controls.Add(loadLinked);

            Button loadZone = new Button();
            loadZone.Text = "Whole Zone";
            loadZone.Left = 620;
            loadZone.Top = 9;
            loadZone.Width = 100;
            loadZone.Click += delegate { LoadWholeZone(); };
            top.Controls.Add(loadZone);

            Button reset = new Button();
            reset.Text = "Reset View";
            reset.Left = 730;
            reset.Top = 9;
            reset.Width = 80;
            reset.Click += delegate { if (_levelCanvas != null) _levelCanvas.ResetView(); };
            top.Controls.Add(reset);

            _levelViewMode = new ComboBox();
            _levelViewMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _levelViewMode.Left = 820;
            _levelViewMode.Top = 9;
            _levelViewMode.Width = 90;
            _levelViewMode.Items.Add("Top-down");
            _levelViewMode.Items.Add("3D orbit");
            _levelViewMode.SelectedIndex = 0;
            _levelViewMode.SelectedIndexChanged += delegate { UpdateLevelViewMode(); };
            top.Controls.Add(_levelViewMode);

            _showTerrain = new CheckBox();
            _showTerrain.Text = "Terrain";
            _showTerrain.Checked = true;
            _showTerrain.Left = 920;
            _showTerrain.Top = 13;
            _showTerrain.Width = 70;
            _showTerrain.CheckedChanged += delegate { UpdateLevelLayerVisibility(); };
            top.Controls.Add(_showTerrain);

            _showObjects = new CheckBox();
            _showObjects.Text = "Objects";
            _showObjects.Checked = true;
            _showObjects.Left = 995;
            _showObjects.Top = 13;
            _showObjects.Width = 70;
            _showObjects.CheckedChanged += delegate { UpdateLevelLayerVisibility(); };
            top.Controls.Add(_showObjects);

            _showPortals = new CheckBox();
            _showPortals.Text = "Portals";
            _showPortals.Checked = true;
            _showPortals.Left = 1070;
            _showPortals.Top = 13;
            _showPortals.Width = 70;
            _showPortals.CheckedChanged += delegate { UpdateLevelLayerVisibility(); };
            top.Controls.Add(_showPortals);

            _dragObjects = new CheckBox();
            _dragObjects.Text = "Drag";
            _dragObjects.Checked = true;
            _dragObjects.Left = 1145;
            _dragObjects.Top = 13;
            _dragObjects.Width = 55;
            _dragObjects.CheckedChanged += delegate { UpdateLevelViewMode(); };
            top.Controls.Add(_dragObjects);

            SplitContainer levelSplit = new SplitContainer();
            levelSplit.Dock = DockStyle.Fill;
            levelSplit.Panel1MinSize = 80;
            levelSplit.Panel2MinSize = 80;
            levelTab.Controls.Add(levelSplit);
            levelSplit.BringToFront();

            SplitContainer leftSplit = new SplitContainer();
            leftSplit.Dock = DockStyle.Fill;
            leftSplit.Orientation = Orientation.Horizontal;
            leftSplit.Panel1MinSize = 60;
            leftSplit.Panel2MinSize = 60;
            levelSplit.Panel1.Controls.Add(leftSplit);

            _levelObjects = new ListView();
            _levelObjects.Dock = DockStyle.Fill;
            _levelObjects.View = View.Details;
            _levelObjects.FullRowSelect = true;
            _levelObjects.GridLines = true;
            _levelObjects.Columns.Add("Room", 75);
            _levelObjects.Columns.Add("Object", 210);
            _levelObjects.Columns.Add("Code", 70);
            _levelObjects.Columns.Add("ID", 55);
            _levelObjects.Columns.Add("X", 55);
            _levelObjects.Columns.Add("Y", 55);
            _levelObjects.Columns.Add("Z", 55);
            _levelObjects.Columns.Add("Model", 55);
            _levelObjects.Columns.Add("Edit", 80);
            _levelObjects.SelectedIndexChanged += delegate { SelectLevelObjectFromList(); };
            leftSplit.Panel1.Controls.Add(_levelObjects);

            TabControl lowerTabs = new TabControl();
            lowerTabs.Dock = DockStyle.Fill;
            leftSplit.Panel2.Controls.Add(lowerTabs);

            TabPage propsTab = new TabPage("Object");
            lowerTabs.TabPages.Add(propsTab);
            BuildObjectPropertyPanel(propsTab);

            TabPage cameraTab = new TabPage("Camera");
            lowerTabs.TabPages.Add(cameraTab);
            BuildCameraPanel(cameraTab);

            TabPage portalsTab = new TabPage("Portals");
            lowerTabs.TabPages.Add(portalsTab);
            _levelPortals = new ListView();
            _levelPortals.Dock = DockStyle.Fill;
            _levelPortals.View = View.Details;
            _levelPortals.FullRowSelect = true;
            _levelPortals.GridLines = true;
            _levelPortals.Columns.Add("To Room", 125);
            _levelPortals.Columns.Add("Signal", 60);
            _levelPortals.Columns.Add("Min", 95);
            _levelPortals.Columns.Add("Max", 95);
            portalsTab.Controls.Add(_levelPortals);

            TabPage summaryTab = new TabPage("Summary");
            lowerTabs.TabPages.Add(summaryTab);
            _levelSummary = new TextBox();
            _levelSummary.Dock = DockStyle.Fill;
            _levelSummary.Multiline = true;
            _levelSummary.ReadOnly = true;
            _levelSummary.ScrollBars = ScrollBars.Both;
            _levelSummary.Font = new Font("Consolas", 9F);
            summaryTab.Controls.Add(_levelSummary);

            _levelCanvas = new LevelCanvas();
            _levelCanvas.Dock = DockStyle.Fill;
            _levelCanvas.SelectedObjectChanged += delegate { SelectLevelObjectFromCanvas(); };
            _levelCanvas.ObjectMoved += delegate { RefreshSelectedLevelObjectAfterCanvasMove(); };
            _levelCanvas.PortalActivated += delegate(object sender, PortalActivatedEventArgs args) { OpenPortalTarget(args.Document, args.Portal); };
            _levelCanvas.DragObjects = true;
            levelSplit.Panel2.Controls.Add(_levelCanvas);
        }

        private void BuildObjectPropertyPanel(Control parent)
        {
            ScrollableControl scrollParent = parent as ScrollableControl;
            if (scrollParent != null) scrollParent.AutoScroll = true;

            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Top;
            panel.ColumnCount = 2;
            panel.RowCount = 8;
            panel.Padding = new Padding(10);
            panel.AutoSize = true;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            parent.Controls.Add(panel);

            _objX = AddNumeric(panel, "X", 0);
            _objY = AddNumeric(panel, "Y", 1);
            _objZ = AddNumeric(panel, "Z", 2);
            _rotX = AddNumeric(panel, "Rot X", 3);
            _rotY = AddNumeric(panel, "Rot Y", 4);
            _rotZ = AddNumeric(panel, "Rot Z", 5);

            TableLayoutPanel actions = new TableLayoutPanel();
            actions.Dock = DockStyle.Top;
            actions.ColumnCount = 2;
            actions.RowCount = 3;
            actions.AutoSize = true;
            actions.Margin = new Padding(0, 6, 0, 0);
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panel.Controls.Add(actions, 0, 6);
            panel.SetColumnSpan(actions, 2);

            ToolTip actionTips = new ToolTip();

            Button apply = new Button();
            apply.Text = "Apply";
            apply.Dock = DockStyle.Fill;
            apply.Margin = new Padding(3);
            apply.Click += delegate { ApplySelectedObjectValues(); };
            actionTips.SetToolTip(apply, "Apply the X/Y/Z and rotation values to the selected object.");
            actions.Controls.Add(apply, 0, 0);

            Button snapY = new Button();
            snapY.Text = "Snap Y";
            snapY.Dock = DockStyle.Fill;
            snapY.Margin = new Padding(3);
            snapY.Click += delegate { SnapSelectedObjectYToTerrain(); };
            actionTips.SetToolTip(snapY, "Set the selected object's Y to the terrain height under its current X/Z.");
            actions.Controls.Add(snapY, 1, 0);

            Button revertSelected = new Button();
            revertSelected.Text = "Revert";
            revertSelected.Dock = DockStyle.Fill;
            revertSelected.Margin = new Padding(3);
            revertSelected.Click += delegate { RevertSelectedObjectValues(); };
            actionTips.SetToolTip(revertSelected, "Restore the selected object to its original placement.");
            actions.Controls.Add(revertSelected, 0, 1);

            Button revertAll = new Button();
            revertAll.Text = "Revert All";
            revertAll.Dock = DockStyle.Fill;
            revertAll.Margin = new Padding(3);
            revertAll.Click += delegate { RevertAllObjectEdits(); };
            actionTips.SetToolTip(revertAll, "Restore every edited object in the loaded room set.");
            actions.Controls.Add(revertAll, 1, 1);

            Button saveSelected = new Button();
            saveSelected.Text = "Save Selected";
            saveSelected.Dock = DockStyle.Fill;
            saveSelected.Margin = new Padding(3);
            saveSelected.Click += delegate { SaveSelectedObjectPatchAsCopy(); };
            actionTips.SetToolTip(saveSelected, "Write a patched BIN where only the selected object edit is included.");
            actions.Controls.Add(saveSelected, 0, 2);

            Button save = new Button();
            save.Text = "Save All";
            save.Dock = DockStyle.Fill;
            save.Margin = new Padding(3);
            save.Click += delegate { SaveCurrentLevelPatchAsCopy(); };
            actionTips.SetToolTip(save, "Write a patched BIN with all object edits in the selected room.");
            actions.Controls.Add(save, 1, 2);

            _objectDelta = new Label();
            _objectDelta.Dock = DockStyle.Fill;
            _objectDelta.AutoSize = true;
            _objectDelta.Margin = new Padding(3, 8, 3, 3);
            _objectDelta.Text = "No object selected.";
            panel.Controls.Add(_objectDelta, 0, 7);
            panel.SetColumnSpan(_objectDelta, 2);
        }

        private void BuildCameraPanel(Control parent)
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Top;
            panel.ColumnCount = 2;
            panel.RowCount = 6;
            panel.Padding = new Padding(10);
            panel.AutoSize = true;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            parent.Controls.Add(panel);

            AddCameraButton(panel, "3D Isometric", 0, 0, delegate { SetCameraPreset(0.75f, 0.62f); });
            AddCameraButton(panel, "Front", 1, 0, delegate { SetCameraPreset(0.0f, 0.0f); });
            AddCameraButton(panel, "Back", 0, 1, delegate { SetCameraPreset((float)Math.PI, 0.0f); });
            AddCameraButton(panel, "Left Side", 1, 1, delegate { SetCameraPreset((float)(-Math.PI / 2.0), 0.0f); });
            AddCameraButton(panel, "Right Side", 0, 2, delegate { SetCameraPreset((float)(Math.PI / 2.0), 0.0f); });
            AddCameraButton(panel, "High Angle", 1, 2, delegate { SetCameraPreset(0.75f, 1.05f); });
            AddCameraButton(panel, "Rotate Left", 0, 3, delegate { NudgeCamera(-0.22f, 0.0f); });
            AddCameraButton(panel, "Rotate Right", 1, 3, delegate { NudgeCamera(0.22f, 0.0f); });
            AddCameraButton(panel, "Tilt Up", 0, 4, delegate { NudgeCamera(0.0f, -0.18f); });
            AddCameraButton(panel, "Tilt Down", 1, 4, delegate { NudgeCamera(0.0f, 0.18f); });
            AddCameraButton(panel, "Reset View", 0, 5, delegate { if (_levelCanvas != null) _levelCanvas.ResetView(); });
            AddCameraButton(panel, "3D Mode", 1, 5, delegate { EnsureOrbitViewMode(); });
        }

        private void AddCameraButton(TableLayoutPanel panel, string text, int column, int row, EventHandler handler)
        {
            Button button = new Button();
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.Height = 28;
            button.Margin = new Padding(3);
            button.Click += handler;
            panel.Controls.Add(button, column, row);
        }

        private void SetCameraPreset(float yaw, float pitch)
        {
            EnsureOrbitViewMode();
            if (_levelCanvas != null) _levelCanvas.SetCameraAngles(yaw, pitch);
        }

        private void NudgeCamera(float yawDelta, float pitchDelta)
        {
            EnsureOrbitViewMode();
            if (_levelCanvas != null) _levelCanvas.RotateCamera(yawDelta, pitchDelta);
        }

        private void EnsureOrbitViewMode()
        {
            if (_levelViewMode != null && _levelViewMode.SelectedIndex != 1)
            {
                _levelViewMode.SelectedIndex = 1;
            }
            else
            {
                UpdateLevelViewMode();
            }
        }

        private NumericUpDown AddNumeric(TableLayoutPanel panel, string label, int row)
        {
            Label l = new Label();
            l.Text = label;
            l.AutoSize = true;
            l.Anchor = AnchorStyles.Left;
            panel.Controls.Add(l, 0, row);

            NumericUpDown n = new NumericUpDown();
            n.Minimum = short.MinValue;
            n.Maximum = short.MaxValue;
            n.DecimalPlaces = 0;
            n.Dock = DockStyle.Fill;
            panel.Controls.Add(n, 1, row);
            return n;
        }

        private void BuildEditorTab(TabPage editorTab)
        {
            TabControl inner = new TabControl();
            inner.Dock = DockStyle.Fill;
            editorTab.Controls.Add(inner);

            TabPage objects = new TabPage("Enemies & Pickups");
            inner.TabPages.Add(objects);
            _objectCandidates = CreateListView();
            objects.Controls.Add(_objectCandidates);

            TabPage terrain = new TabPage("Terrain");
            inner.TabPages.Add(terrain);
            _terrainCandidates = CreateListView();
            terrain.Controls.Add(_terrainCandidates);

            TabPage palettes = new TabPage("Palettes");
            inner.TabPages.Add(palettes);
            _paletteCandidates = CreateListView();
            palettes.Controls.Add(_paletteCandidates);

            TabPage audio = new TabPage("Area Music & Audio");
            inner.TabPages.Add(audio);
            _audioCandidates = CreateListView();
            audio.Controls.Add(_audioCandidates);
        }

        private void OpenImageDialog()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "Disc images (*.cue;*.bin)|*.cue;*.bin|All files (*.*)|*.*";
                dlg.Title = "Open Soul Reaver BIN/CUE";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    OpenImage(dlg.FileName);
                }
            }
        }

        private void OpenImage(string path)
        {
            try
            {
                SetStatus("Opening " + path);
                CloseImage();
                _disc = DiscImage.Open(path);
                _isoRoot = Iso9660Reader.Read(_disc);
                _bigIsoEntry = FindIsoEntry(_isoRoot, "/BIGFILE.DAT");
                if (_bigIsoEntry != null)
                {
                    _bigFileStream = _disc.OpenFile(_bigIsoEntry);
                    _bigFile = new BigFileReader(_bigFileStream, true);
                }
                BuildTree();
                PopulateResearchTabs();
                PopulateLevelSelector();
                SetStatus("Loaded " + Path.GetFileName(_disc.ImagePath));
            }
            catch (Exception ex)
            {
                CloseImage();
                MessageBox.Show(this, ex.Message, "Open failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Open failed");
            }
        }

        private void CloseImage()
        {
            if (_bigFile != null) _bigFile.Dispose();
            _bigFile = null;
            if (_bigFileStream != null) _bigFileStream.Dispose();
            _bigFileStream = null;
            if (_disc != null) _disc.Dispose();
            _disc = null;
            _isoRoot = null;
            _bigIsoEntry = null;
            if (_tree != null) _tree.Nodes.Clear();
            ClearViews();
            ClearLevelEditor();
        }

        private void BuildTree()
        {
            _tree.BeginUpdate();
            try
            {
                _tree.Nodes.Clear();
                TreeNode rootNode = new TreeNode("Disc image");
                rootNode.Tag = _isoRoot;
                _tree.Nodes.Add(rootNode);
                AddIsoChildren(rootNode, _isoRoot);
                rootNode.Expand();
            }
            finally
            {
                _tree.EndUpdate();
            }
        }

        private void AddIsoChildren(TreeNode parentNode, IsoFileEntry parentEntry)
        {
            foreach (IsoFileEntry child in parentEntry.Children)
            {
                TreeNode node = new TreeNode(child.Name);
                node.Tag = child;
                parentNode.Nodes.Add(node);
                if (child.IsDirectory)
                {
                    AddIsoChildren(node, child);
                }
                else if (child.FullPath.Equals("/BIGFILE.DAT", StringComparison.OrdinalIgnoreCase) && _bigFile != null)
                {
                    TreeNode bigRoot = new TreeNode("Internal BIGFILE index");
                    bigRoot.Tag = _bigFile;
                    node.Nodes.Add(bigRoot);
                    foreach (BigFileFolder folder in _bigFile.Folders)
                    {
                        TreeNode folderNode = new TreeNode(folder.DisplayName);
                        folderNode.Tag = folder;
                        bigRoot.Nodes.Add(folderNode);
                        foreach (BigFileEntry entry in folder.Files)
                        {
                            TreeNode fileNode = new TreeNode(entry.DisplayName);
                            fileNode.Tag = entry;
                            folderNode.Nodes.Add(fileNode);
                        }
                    }
                }
            }
        }

        private void DisplaySelection()
        {
            if (_tree.SelectedNode == null) return;
            object tag = _tree.SelectedNode.Tag;
            ClearViews();

            IsoFileEntry iso = tag as IsoFileEntry;
            if (iso != null)
            {
                DisplayIsoEntry(iso);
                return;
            }

            BigFileReader big = tag as BigFileReader;
            if (big != null)
            {
                DisplayBigFileRoot(big);
                return;
            }

            BigFileFolder folder = tag as BigFileFolder;
            if (folder != null)
            {
                DisplayBigFolder(folder);
                return;
            }

            BigFileEntry entry = tag as BigFileEntry;
            if (entry != null)
            {
                DisplayBigEntry(entry);
            }
        }

        private void DisplayIsoEntry(IsoFileEntry entry)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ISO9660 resource");
            sb.AppendLine("Path: " + entry.FullPath);
            sb.AppendLine("Type: " + (entry.IsDirectory ? "Directory" : "File"));
            sb.AppendLine("LBA: " + entry.Lba);
            sb.AppendLine("Size: " + entry.Size + " (" + Util.FormatSize(entry.Size) + ")");
            if (_disc != null)
            {
                long raw = (long)entry.Lba * _disc.RawSectorSize + _disc.UserDataOffset;
                sb.AppendLine("Raw image offset: 0x" + raw.ToString("X"));
            }
            if (entry.FullPath.Equals("/BIGFILE.DAT", StringComparison.OrdinalIgnoreCase) && _bigFile != null)
            {
                sb.AppendLine();
                sb.AppendLine("Soul Reaver BIGFILE folders: " + _bigFile.Folders.Count);
                sb.AppendLine("Soul Reaver BIGFILE entries: " + CountBigFileEntries());
            }
            if (!entry.IsDirectory)
            {
                sb.AppendLine();
                sb.AppendLine("Editing: use same-size replacement into a new BIN copy for safe asset experiments.");
            }
            _details.Text = sb.ToString();

            foreach (IsoFileEntry child in entry.Children)
            {
                ListViewItem item = new ListViewItem(child.Name);
                item.SubItems.Add(child.IsDirectory ? "Directory" : "File");
                item.SubItems.Add(Util.FormatSize(child.Size));
                item.SubItems.Add("LBA " + child.Lba);
                item.SubItems.Add(child.FullPath);
                item.Tag = child;
                _list.Items.Add(item);
            }

            if (!entry.IsDirectory)
            {
                using (Stream s = _disc.OpenFile(entry))
                {
                    PreviewStream(s, (int)Math.Min(entry.Size, 1024 * 1024));
                }
            }
        }

        private void DisplayBigFileRoot(BigFileReader big)
        {
            _details.Text = "Soul Reaver BIGFILE.DAT internal archive" + Environment.NewLine +
                            "Folders: " + big.Folders.Count + Environment.NewLine +
                            "Files: " + CountBigFileEntries() + Environment.NewLine +
                            "Names are not stored in the archive, so entries are shown by folder/file index and hash fields." + Environment.NewLine;

            foreach (BigFileFolder folder in big.Folders)
            {
                AddFolderListItem(folder);
            }
        }

        private void DisplayBigFolder(BigFileFolder folder)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("BIGFILE folder");
            sb.AppendLine("Folder: " + folder.Index.ToString("X4"));
            sb.AppendLine("Files: " + folder.Files.Count);
            sb.AppendLine("Folder table unknown: 0x" + ((ushort)folder.Unknown).ToString("X4"));
            sb.AppendLine("Offset: 0x" + folder.Offset.ToString("X8"));
            sb.AppendLine("XOR key: 0x" + folder.XorKey.ToString("X4"));
            _details.Text = sb.ToString();

            foreach (BigFileEntry entry in folder.Files)
            {
                AddBigEntryListItem(entry);
            }
        }

        private void DisplayBigEntry(BigFileEntry entry)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("BIGFILE entry");
            sb.AppendLine("Virtual path: " + entry.VirtualPath);
            sb.AppendLine("Kind: " + entry.Kind);
            sb.AppendLine("Size: " + entry.Size + " (" + Util.FormatSize(entry.Size) + ")");
            sb.AppendLine("Offset: 0x" + entry.Offset.ToString("X8"));
            sb.AppendLine("Hash/unknown 1: 0x" + entry.Hash1.ToString("X8"));
            sb.AppendLine("Hash/unknown 2: 0x" + entry.Hash2.ToString("X8"));
            if (!string.IsNullOrEmpty(entry.Notes)) sb.AppendLine("Notes: " + entry.Notes);
            sb.AppendLine();
            sb.AppendLine("Editing: same-size replacement can patch this entry into a new BIN copy.");
            _details.Text = sb.ToString();

            using (Stream s = _bigFile.OpenFile(entry))
            {
                PreviewStream(s, (int)Math.Min(entry.Size, 1024 * 1024));
            }
        }

        private void AddFolderListItem(BigFileFolder folder)
        {
            ListViewItem item = new ListViewItem("folder" + folder.Index.ToString("X4"));
            item.SubItems.Add("BIGFILE folder");
            item.SubItems.Add(folder.Files.Count + " files");
            item.SubItems.Add("0x" + folder.Offset.ToString("X8"));
            item.SubItems.Add("XOR 0x" + folder.XorKey.ToString("X4"));
            item.Tag = folder;
            _list.Items.Add(item);
        }

        private void AddBigEntryListItem(BigFileEntry entry)
        {
            ListViewItem item = new ListViewItem("file" + entry.FileIndex.ToString("X4"));
            item.SubItems.Add(entry.Kind);
            item.SubItems.Add(Util.FormatSize(entry.Size));
            item.SubItems.Add("0x" + entry.Offset.ToString("X8"));
            item.SubItems.Add(entry.Notes ?? "");
            item.Tag = entry;
            _list.Items.Add(item);
        }

        private void PreviewStream(Stream stream, int stringScanLimit)
        {
            stream.Position = 0;
            byte[] head = Util.ReadUpTo(stream, 4096);
            _hex.Text = Util.HexDump(head, head.Length);
            stream.Position = 0;
            List<string> strings = Util.ExtractStrings(stream, stringScanLimit, 5);
            StringBuilder sb = new StringBuilder();
            foreach (string s in strings)
            {
                sb.AppendLine(s);
            }
            _strings.Text = sb.ToString();
        }

        private void PopulateResearchTabs()
        {
            _objectCandidates.Items.Clear();
            _terrainCandidates.Items.Clear();
            _paletteCandidates.Items.Clear();
            _audioCandidates.Items.Clear();

            if (_bigFile != null)
            {
                foreach (BigFileEntry entry in _bigFile.AllFiles())
                {
                    string kind = entry.Kind ?? "";
                    if (kind.IndexOf("TIM", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        AddResearchItem(_paletteCandidates, entry);
                    }
                    if (kind.IndexOf("audio", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        kind.IndexOf("sound", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        kind.IndexOf("sequence", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        kind.IndexOf("samples", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        AddResearchItem(_audioCandidates, entry);
                    }
                    if (kind.IndexOf("room", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        kind.IndexOf("metadata", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (entry.Size > 4096 && entry.Size < 200000 && kind == "Unknown"))
                    {
                        AddResearchItem(_terrainCandidates, entry);
                    }
                    if (kind.IndexOf("metadata", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (entry.Size <= 4096 && kind == "Unknown"))
                    {
                        AddResearchItem(_objectCandidates, entry);
                    }
                }
            }

            if (_isoRoot != null)
            {
                AddIsoAudioCandidates(_isoRoot);
            }
        }

        private void PopulateLevelSelector()
        {
            if (_levelSelector == null) return;
            _levelSelector.Items.Clear();
            if (_bigFile == null) return;

            List<LevelComboItem> items = new List<LevelComboItem>();
            foreach (BigFileEntry entry in _bigFile.AllFiles())
            {
                LevelProbe probe;
                using (Stream s = _bigFile.OpenFile(entry))
                {
                    if (!SR1LevelParser.TryProbe(s, entry, out probe)) continue;
                }

                string text = string.Format("{0}  folder {1:X4}/file {2:X4}  obj:{3} verts:{4} faces:{5}",
                    ZoneNamer.DisplayName(probe.Name),
                    entry.FolderIndex,
                    entry.FileIndex,
                    probe.IntroCount,
                    probe.VertexCount,
                    probe.PolygonCount);
                items.Add(new LevelComboItem(text, entry));
            }

            items.Sort(delegate(LevelComboItem a, LevelComboItem b)
            {
                return string.Compare(a.Text, b.Text, StringComparison.CurrentCultureIgnoreCase);
            });

            foreach (LevelComboItem item in items)
            {
                _levelSelector.Items.Add(item);
            }

            if (_levelSelector.Items.Count > 0) _levelSelector.SelectedIndex = 0;
        }

        private void LoadSelectedLevelFromCombo()
        {
            LevelComboItem item = _levelSelector == null ? null : _levelSelector.SelectedItem as LevelComboItem;
            if (item == null)
            {
                MessageBox.Show(this, "No room is selected.", "Level editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            LoadLevelEntry(item.Entry);
        }

        private void LoadLevelFromTreeSelection()
        {
            BigFileEntry entry = _tree.SelectedNode == null ? null : _tree.SelectedNode.Tag as BigFileEntry;
            if (entry == null)
            {
                MessageBox.Show(this, "Select a BIGFILE room/unit entry first.", "Level editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            LoadLevelEntry(entry);
        }

        private void LoadLevelEntry(BigFileEntry entry)
        {
            if (_bigFile == null || entry == null) return;
            try
            {
                SetStatus("Loading level " + entry.VirtualPath);
                string error;
                SR1LevelDocument doc;
                using (Stream s = _bigFile.OpenFile(entry))
                {
                    if (!SR1LevelParser.TryParse(s, entry, out doc, out error))
                    {
                        MessageBox.Show(this, error, "Level parse failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                _currentLevel = doc;
                _selectedLevelDocument = doc;
                _loadedLevels.Clear();
                _loadedLevels.Add(doc);
                PopulateLevelDocumentViews();
                SetStatus("Loaded level " + doc.Summary);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Level load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenPortalTarget(SR1LevelDocument sourceDocument, LevelPortal portal)
        {
            if (_bigFile == null || portal == null) return;

            string targetName = ZoneNamer.NormalizeRoomName(portal.ToLevelName);
            if (string.IsNullOrEmpty(targetName))
            {
                MessageBox.Show(this, "This portal does not have a decoded target room name yet.", "Portal", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SR1LevelDocument alreadyLoaded = FindLoadedLevelByName(targetName);
            if (alreadyLoaded != null)
            {
                _currentLevel = alreadyLoaded;
                _selectedLevelDocument = alreadyLoaded;
                PopulateLevelDocumentViews();
                SetStatus("Focused portal target " + ZoneNamer.DisplayName(alreadyLoaded.Name));
                return;
            }

            try
            {
                SetStatus("Opening portal target " + targetName);
                Dictionary<string, List<BigFileEntry>> levelsByName = BuildLevelNameMap();
                int sourceFolder = sourceDocument != null && sourceDocument.SourceEntry != null
                    ? sourceDocument.SourceEntry.FolderIndex
                    : (_currentLevel != null && _currentLevel.SourceEntry != null ? _currentLevel.SourceEntry.FolderIndex : 0);
                BigFileEntry entry = FindLinkedLevelEntry(levelsByName, targetName, sourceFolder);
                if (entry == null)
                {
                    MessageBox.Show(this,
                        "Could not find a decoded room entry for portal target \"" + targetName + "\".",
                        "Portal",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                LoadLevelEntry(entry);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Portal load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLinkedRooms()
        {
            if (_currentLevel == null)
            {
                LoadSelectedLevelFromCombo();
                if (_currentLevel == null) return;
            }

            try
            {
                SetStatus("Following portals from " + _currentLevel.Name);
                Dictionary<string, List<BigFileEntry>> levelsByName = BuildLevelNameMap();
                Queue<SR1LevelDocument> queue = new Queue<SR1LevelDocument>();
                HashSet<string> loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _loadedLevels.Clear();
                _loadedLevels.Add(_currentLevel);
                queue.Enqueue(_currentLevel);
                loadedNames.Add(ZoneNamer.NormalizeRoomName(_currentLevel.Name ?? ""));

                while (queue.Count > 0 && _loadedLevels.Count < 64)
                {
                    SR1LevelDocument doc = queue.Dequeue();
                    foreach (LevelPortal portal in doc.Portals)
                    {
                        string name = ZoneNamer.NormalizeRoomName(portal.ToLevelName);
                        if (string.IsNullOrEmpty(name) || loadedNames.Contains(name)) continue;
                        BigFileEntry entry = FindLinkedLevelEntry(levelsByName, name, doc.SourceEntry.FolderIndex);
                        if (entry == null) continue;

                        string error;
                        SR1LevelDocument linked;
                        using (Stream s = _bigFile.OpenFile(entry))
                        {
                            if (!SR1LevelParser.TryParse(s, entry, out linked, out error)) continue;
                        }

                        _loadedLevels.Add(linked);
                        queue.Enqueue(linked);
                        loadedNames.Add(ZoneNamer.NormalizeRoomName(linked.Name ?? name));
                    }
                }

                _selectedLevelDocument = _currentLevel;
                PopulateLevelDocumentViews();
                SetStatus("Loaded " + _loadedLevels.Count + " linked room(s)");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Linked room load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadWholeZone()
        {
            if (_currentLevel == null)
            {
                LoadSelectedLevelFromCombo();
                if (_currentLevel == null) return;
            }

            try
            {
                string zone = ZoneNamer.FriendlyZone(_currentLevel.Name);
                if (zone == "Unknown / Unmapped")
                {
                    MessageBox.Show(this, "This room prefix is not mapped to a friendly zone yet.", "Whole zone", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                SetStatus("Loading whole zone: " + zone);
                Dictionary<string, List<BigFileEntry>> levelsByName = BuildLevelNameMap();
                List<string> roomNames = new List<string>();
                foreach (string name in levelsByName.Keys)
                {
                    if (string.Equals(ZoneNamer.FriendlyZone(name), zone, StringComparison.OrdinalIgnoreCase))
                    {
                        roomNames.Add(name);
                    }
                }
                roomNames.Sort(StringComparer.CurrentCultureIgnoreCase);

                _loadedLevels.Clear();
                HashSet<string> loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int sourceFolder = _currentLevel.SourceEntry.FolderIndex;
                foreach (string name in roomNames)
                {
                    BigFileEntry entry = FindLinkedLevelEntry(levelsByName, name, sourceFolder);
                    if (entry == null || loadedNames.Contains(name)) continue;

                    string error;
                    SR1LevelDocument doc;
                    using (Stream s = _bigFile.OpenFile(entry))
                    {
                        if (!SR1LevelParser.TryParse(s, entry, out doc, out error)) continue;
                    }

                    _loadedLevels.Add(doc);
                    loadedNames.Add(ZoneNamer.NormalizeRoomName(doc.Name ?? name));
                }

                if (_loadedLevels.Count == 0)
                {
                    _loadedLevels.Add(_currentLevel);
                }

                _selectedLevelDocument = FindLoadedLevelByName(ZoneNamer.NormalizeRoomName(_currentLevel.Name)) ?? _loadedLevels[0];
                _currentLevel = _selectedLevelDocument;
                PopulateLevelDocumentViews();
                SetStatus("Loaded " + _loadedLevels.Count + " room(s) for " + zone);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Whole zone load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private SR1LevelDocument FindLoadedLevelByName(string roomName)
        {
            string clean = ZoneNamer.NormalizeRoomName(roomName);
            foreach (SR1LevelDocument doc in _loadedLevels)
            {
                if (string.Equals(ZoneNamer.NormalizeRoomName(doc.Name), clean, StringComparison.OrdinalIgnoreCase))
                {
                    return doc;
                }
            }
            return null;
        }

        private Dictionary<string, List<BigFileEntry>> BuildLevelNameMap()
        {
            Dictionary<string, List<BigFileEntry>> map = new Dictionary<string, List<BigFileEntry>>(StringComparer.OrdinalIgnoreCase);
            if (_bigFile == null) return map;
            foreach (BigFileEntry entry in _bigFile.AllFiles())
            {
                LevelProbe probe;
                using (Stream s = _bigFile.OpenFile(entry))
                {
                    if (!SR1LevelParser.TryProbe(s, entry, out probe)) continue;
                }
                string name = ZoneNamer.NormalizeRoomName(probe.Name);
                if (string.IsNullOrEmpty(name)) continue;
                List<BigFileEntry> entries;
                if (!map.TryGetValue(name, out entries))
                {
                    entries = new List<BigFileEntry>();
                    map.Add(name, entries);
                }
                entries.Add(entry);
            }
            return map;
        }

        private static BigFileEntry FindLinkedLevelEntry(Dictionary<string, List<BigFileEntry>> map, string name, int sourceFolder)
        {
            List<BigFileEntry> entries;
            if (!map.TryGetValue(name, out entries) || entries.Count == 0) return null;
            BigFileEntry best = entries[0];
            int bestDistance = Math.Abs(best.FolderIndex - sourceFolder);
            foreach (BigFileEntry entry in entries)
            {
                int distance = Math.Abs(entry.FolderIndex - sourceFolder);
                if (distance < bestDistance)
                {
                    best = entry;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private void PopulateLevelDocumentViews()
        {
            _levelObjects.Items.Clear();
            _levelPortals.Items.Clear();
            if (_currentLevel == null)
            {
                _levelCanvas.Document = null;
                _levelSummary.Clear();
                return;
            }

            if (_loadedLevels.Count == 0) _loadedLevels.Add(_currentLevel);

            foreach (SR1LevelDocument doc in _loadedLevels)
            {
                foreach (LevelObjectPlacement obj in doc.Objects)
                {
                    AddLevelObjectListItem(doc, obj);
                }
            }

            foreach (SR1LevelDocument doc in _loadedLevels)
            {
                foreach (LevelPortal portal in doc.Portals)
                {
                    ListViewItem item = new ListViewItem(ZoneNamer.DisplayPortalTarget(portal.ToLevelName));
                    item.SubItems.Add(portal.SignalId.ToString());
                    item.SubItems.Add(string.Format("{0},{1},{2}", portal.MinX, portal.MinY, portal.MinZ));
                    item.SubItems.Add(string.Format("{0},{1},{2}", portal.MaxX, portal.MaxY, portal.MaxZ));
                    item.Tag = portal;
                    _levelPortals.Items.Add(item);
                }
            }

            RefreshLevelSummaryText();

            _levelCanvas.SetScene(_loadedLevels, _selectedLevelDocument ?? _currentLevel);
            UpdateLevelLayerVisibility();
            UpdateLevelViewMode();
            if (_levelObjects.Items.Count > 0) _levelObjects.Items[0].Selected = true;
        }

        private void AddLevelObjectListItem(SR1LevelDocument doc, LevelObjectPlacement obj)
        {
            ListViewItem item = new ListViewItem(ZoneNamer.NormalizeRoomName(doc.Name));
            item.SubItems.Add(ObjectNamer.FriendlyName(obj.FileName));
            item.SubItems.Add(ObjectNamer.Normalize(obj.FileName));
            item.SubItems.Add(obj.UniqueId.ToString());
            item.SubItems.Add(obj.X.ToString());
            item.SubItems.Add(obj.Y.ToString());
            item.SubItems.Add(obj.Z.ToString());
            item.SubItems.Add(obj.ModelIndex.ToString());
            item.SubItems.Add(obj.HasChanged ? "Changed" : "");
            item.Tag = new LevelObjectTag(doc, obj);
            _levelObjects.Items.Add(item);
        }

        private void UpdateLevelLayerVisibility()
        {
            if (_levelCanvas == null) return;
            _levelCanvas.ShowTerrain = _showTerrain == null || _showTerrain.Checked;
            _levelCanvas.ShowObjects = _showObjects == null || _showObjects.Checked;
            _levelCanvas.ShowPortals = _showPortals == null || _showPortals.Checked;
            _levelCanvas.Invalidate();
        }

        private void UpdateLevelViewMode()
        {
            if (_levelCanvas == null) return;
            _levelCanvas.ViewMode = _levelViewMode != null && _levelViewMode.SelectedIndex == 1 ? LevelViewMode.Orbit3D : LevelViewMode.TopDown;
            _levelCanvas.DragObjects = _dragObjects != null && _dragObjects.Checked;
            _levelCanvas.Invalidate();
        }

        private void SelectLevelObjectFromList()
        {
            if (_levelObjects.SelectedItems.Count == 0 || _currentLevel == null) return;
            LevelObjectTag tag = _levelObjects.SelectedItems[0].Tag as LevelObjectTag;
            if (tag == null) return;
            _selectedLevelDocument = tag.Document;
            _levelCanvas.SelectObject(tag.Document, tag.Object.Index);
            LoadObjectIntoEditors(tag.Document, tag.Object);
        }

        private void SelectLevelObjectFromCanvas()
        {
            if (_currentLevel == null || _levelCanvas.SelectedObjectIndex < 0 || _levelCanvas.SelectedDocument == null) return;
            int index = _levelCanvas.SelectedObjectIndex;
            _selectedLevelDocument = _levelCanvas.SelectedDocument;
            for (int i = 0; i < _levelObjects.Items.Count; i++)
            {
                LevelObjectTag tag = _levelObjects.Items[i].Tag as LevelObjectTag;
                if (tag != null && tag.Document == _selectedLevelDocument && tag.Object.Index == index)
                {
                    _levelObjects.SelectedItems.Clear();
                    _levelObjects.Items[i].Selected = true;
                    _levelObjects.Items[i].EnsureVisible();
                    LoadObjectIntoEditors(tag.Document, tag.Object);
                    break;
                }
            }
        }

        private void RefreshSelectedLevelObjectAfterCanvasMove()
        {
            if (_levelCanvas == null || _levelCanvas.SelectedDocument == null || _levelCanvas.SelectedObjectIndex < 0) return;
            SelectLevelObjectFromCanvas();
            if (_levelObjects.SelectedItems.Count > 0)
            {
                LevelObjectTag tag = _levelObjects.SelectedItems[0].Tag as LevelObjectTag;
                if (tag != null)
                {
                    LoadObjectIntoEditors(tag.Document, tag.Object);
                    UpdateSelectedLevelObjectListItem(tag.Object);
                    RefreshLevelSummaryText();
                }
            }
        }

        private void LoadObjectIntoEditors(LevelObjectPlacement obj)
        {
            LoadObjectIntoEditors(FindDocumentForObject(obj), obj);
        }

        private void LoadObjectIntoEditors(SR1LevelDocument document, LevelObjectPlacement obj)
        {
            _objX.Value = ClampNumeric(obj.X, _objX);
            _objY.Value = ClampNumeric(obj.Y, _objY);
            _objZ.Value = ClampNumeric(obj.Z, _objZ);
            _rotX.Value = ClampNumeric(obj.RotationRawX, _rotX);
            _rotY.Value = ClampNumeric(obj.RotationRawY, _rotY);
            _rotZ.Value = ClampNumeric(obj.RotationRawZ, _rotZ);
            if (_objectDelta != null) _objectDelta.Text = BuildObjectDeltaText(document, obj);
            SetStatus("Selected " + ObjectNamer.DisplayName(obj.FileName) + " id " + obj.UniqueId);
        }

        private SR1LevelDocument FindDocumentForObject(LevelObjectPlacement obj)
        {
            if (obj == null) return null;
            foreach (SR1LevelDocument doc in _loadedLevels)
            {
                foreach (LevelObjectPlacement candidate in doc.Objects)
                {
                    if (object.ReferenceEquals(candidate, obj)) return doc;
                }
            }
            return _selectedLevelDocument ?? _currentLevel;
        }

        private static string BuildObjectDeltaText(SR1LevelDocument document, LevelObjectPlacement obj)
        {
            if (obj == null) return "No object selected.";
            string text = string.Format(
                "Object: {0} ({1}) id {2}{3}Current: X {4}, Y {5}, Z {6}{3}Original: X {7}, Y {8}, Z {9}{3}Delta: X {10}, Y {11}, Z {12}",
                ObjectNamer.DisplayName(obj.FileName),
                ObjectNamer.Normalize(obj.FileName),
                obj.UniqueId,
                Environment.NewLine,
                obj.X,
                obj.Y,
                obj.Z,
                obj.OriginalX,
                obj.OriginalY,
                obj.OriginalZ,
                obj.X - obj.OriginalX,
                obj.Y - obj.OriginalY,
                obj.Z - obj.OriginalZ);
            string note = ObjectNamer.PlacementNote(obj.FileName);
            if (!string.IsNullOrEmpty(note))
            {
                text += Environment.NewLine + Environment.NewLine + "Note: " + note;
            }
            string moveSafety = BuildMoveSafetyNote(document, obj);
            if (!string.IsNullOrEmpty(moveSafety))
            {
                text += Environment.NewLine + Environment.NewLine + "Move safety: " + moveSafety;
            }
            return text;
        }

        private static string BuildMoveSafetyNote(SR1LevelDocument document, LevelObjectPlacement obj)
        {
            if (document == null || obj == null || !obj.HasMoved) return null;

            List<string> notes = new List<string>();
            if (!IsInsideExpandedBounds(document.Bounds, obj.X, obj.Z, 256))
            {
                notes.Add("New X/Z is outside this room's decoded terrain/portal bounds. The intro still belongs to this source room, so moving it into another room requires object transfer support instead of an in-place coordinate edit.");
            }

            short terrainY;
            if (!TryFindTerrainYAt(document, obj.X, obj.Z, obj.Y, out terrainY))
            {
                notes.Add("No terrain triangle was found under the new X/Z in this room. That usually means the object is outside the current stream unit's walkable/collidable space.");
            }
            else
            {
                int yDelta = obj.Y - terrainY;
                if (Math.Abs(yDelta) > 512)
                {
                    notes.Add("Nearest terrain Y is " + terrainY + " (" + yDelta + " away); use Snap Y or move back over the floor before testing in-game.");
                }
            }

            LevelPortal portal = FindNearbyPortal(document, obj.X, obj.Y, obj.Z, 768);
            if (portal != null)
            {
                notes.Add("New position is inside or close to the portal/stream boundary for " + ZoneNamer.DisplayPortalTarget(portal.ToLevelName) + ". Crossing that boundary safely needs the object to belong to the destination room's intro/object tables.");
            }

            return notes.Count == 0 ? null : string.Join(" ", notes.ToArray());
        }

        private static bool IsInsideExpandedBounds(RectangleF bounds, short x, short z, int margin)
        {
            if (bounds.IsEmpty) return true;
            return x >= bounds.Left - margin &&
                   x <= bounds.Right + margin &&
                   z >= bounds.Top - margin &&
                   z <= bounds.Bottom + margin;
        }

        private static LevelPortal FindNearbyPortal(SR1LevelDocument document, short x, short y, short z, int margin)
        {
            if (document == null) return null;
            foreach (LevelPortal portal in document.Portals)
            {
                int minX = Math.Min(portal.MinX, portal.MaxX) - margin;
                int maxX = Math.Max(portal.MinX, portal.MaxX) + margin;
                int minY = Math.Min(portal.MinY, portal.MaxY) - margin;
                int maxY = Math.Max(portal.MinY, portal.MaxY) + margin;
                int minZ = Math.Min(portal.MinZ, portal.MaxZ) - margin;
                int maxZ = Math.Max(portal.MinZ, portal.MaxZ) + margin;
                if (x >= minX && x <= maxX && y >= minY && y <= maxY && z >= minZ && z <= maxZ)
                {
                    return portal;
                }
            }
            return null;
        }

        private static decimal ClampNumeric(int value, NumericUpDown numeric)
        {
            if (value < numeric.Minimum) return numeric.Minimum;
            if (value > numeric.Maximum) return numeric.Maximum;
            return value;
        }

        private static short ClampShort(float value)
        {
            if (value < short.MinValue) return short.MinValue;
            if (value > short.MaxValue) return short.MaxValue;
            return (short)Math.Round(value);
        }

        private void ApplySelectedObjectValues()
        {
            if (_currentLevel == null || _levelObjects.SelectedItems.Count == 0) return;
            LevelObjectTag tag = _levelObjects.SelectedItems[0].Tag as LevelObjectTag;
            if (tag == null) return;
            LevelObjectPlacement obj = tag.Object;

            short nextX = (short)_objX.Value;
            short nextY = (short)_objY.Value;
            short nextZ = (short)_objZ.Value;
            int deltaX = nextX - obj.X;
            int deltaY = nextY - obj.Y;
            int deltaZ = nextZ - obj.Z;
            bool moveSpectral = obj.HasSpectralPosition;

            obj.X = nextX;
            obj.Y = nextY;
            obj.Z = nextZ;
            if (moveSpectral)
            {
                obj.SpectralX = AddShortDelta(obj.SpectralX, deltaX);
                obj.SpectralY = AddShortDelta(obj.SpectralY, deltaY);
                obj.SpectralZ = AddShortDelta(obj.SpectralZ, deltaZ);
            }
            obj.RotationRawX = (short)_rotX.Value;
            obj.RotationRawY = (short)_rotY.Value;
            obj.RotationRawZ = (short)_rotZ.Value;
            SR1LevelParser.WriteObjectToRaw(tag.Document, obj);
            UpdateSelectedLevelObjectListItem(obj);
            RefreshLevelSummaryText();
            _selectedLevelDocument = tag.Document;
            _levelCanvas.SelectObject(tag.Document, obj.Index);
            SetStatus("Updated " + obj.Name + " in the working level buffer");
        }

        private void SnapSelectedObjectYToTerrain()
        {
            LevelObjectTag tag = GetSelectedLevelObjectTag();
            if (tag == null || tag.Document == null || tag.Object == null)
            {
                MessageBox.Show(this, "Select an object first.", "Snap Y", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            short terrainY;
            if (!TryFindTerrainYAt(tag.Document, tag.Object.X, tag.Object.Z, tag.Object.Y, out terrainY))
            {
                MessageBox.Show(this,
                    "Could not find a terrain triangle under this object's X/Z position. Try nudging it over visible floor geometry first.",
                    "Snap Y",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            int deltaY = terrainY - tag.Object.Y;
            bool moveSpectral = tag.Object.HasSpectralPosition;
            tag.Object.Y = terrainY;
            if (moveSpectral) tag.Object.SpectralY = AddShortDelta(tag.Object.SpectralY, deltaY);
            SR1LevelParser.WriteObjectToRaw(tag.Document, tag.Object);
            LoadObjectIntoEditors(tag.Document, tag.Object);
            UpdateSelectedLevelObjectListItem(tag.Object);
            RefreshLevelSummaryText();
            if (_levelCanvas != null)
            {
                _levelCanvas.SelectObject(tag.Document, tag.Object.Index);
                _levelCanvas.Invalidate();
            }
            SetStatus("Snapped " + ObjectNamer.DisplayName(tag.Object.FileName) + " id " + tag.Object.UniqueId + " to terrain Y " + terrainY);
        }

        private static bool TryFindTerrainYAt(SR1LevelDocument doc, short x, short z, short currentY, out short terrainY)
        {
            terrainY = 0;
            if (doc == null || doc.Vertices.Count == 0 || doc.Triangles.Count == 0) return false;

            bool found = false;
            float bestY = 0;
            float bestScore = float.MaxValue;
            foreach (LevelTriangle tri in doc.Triangles)
            {
                LevelVertex a = doc.Vertices[tri.A];
                LevelVertex b = doc.Vertices[tri.B];
                LevelVertex c = doc.Vertices[tri.C];
                float y;
                if (!TryInterpolateTriangleY(a, b, c, x, z, out y)) continue;

                float score = Math.Abs(y - currentY);
                if (!found || score < bestScore)
                {
                    found = true;
                    bestScore = score;
                    bestY = y;
                }
            }

            if (!found) return false;
            terrainY = ClampShort(bestY);
            return true;
        }

        private static bool TryInterpolateTriangleY(LevelVertex a, LevelVertex b, LevelVertex c, float x, float z, out float y)
        {
            y = 0;
            float x0 = a.X;
            float z0 = a.Z;
            float x1 = b.X;
            float z1 = b.Z;
            float x2 = c.X;
            float z2 = c.Z;
            float denom = (z1 - z2) * (x0 - x2) + (x2 - x1) * (z0 - z2);
            if (Math.Abs(denom) < 0.001f) return false;

            float w0 = ((z1 - z2) * (x - x2) + (x2 - x1) * (z - z2)) / denom;
            float w1 = ((z2 - z0) * (x - x2) + (x0 - x2) * (z - z2)) / denom;
            float w2 = 1.0f - w0 - w1;
            const float tolerance = 0.015f;
            if (w0 < -tolerance || w1 < -tolerance || w2 < -tolerance) return false;

            y = w0 * a.Y + w1 * b.Y + w2 * c.Y;
            return true;
        }

        private static short AddShortDelta(short value, int delta)
        {
            int next = value + delta;
            if (next < short.MinValue) return short.MinValue;
            if (next > short.MaxValue) return short.MaxValue;
            return (short)next;
        }

        private void UpdateSelectedLevelObjectListItem(LevelObjectPlacement obj)
        {
            if (_levelObjects.SelectedItems.Count == 0) return;
            ListViewItem item = _levelObjects.SelectedItems[0];
            item.SubItems[4].Text = obj.X.ToString();
            item.SubItems[5].Text = obj.Y.ToString();
            item.SubItems[6].Text = obj.Z.ToString();
            item.SubItems[7].Text = obj.ModelIndex.ToString();
            if (item.SubItems.Count > 8) item.SubItems[8].Text = obj.HasChanged ? "Changed" : "";
        }

        private void RefreshLevelSummaryText()
        {
            if (_levelSummary == null) return;
            if (_currentLevel == null)
            {
                _levelSummary.Clear();
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(_loadedLevels.Count == 1 ? _currentLevel.Summary : (_loadedLevels.Count + " linked rooms loaded"));
            sb.AppendLine("Zone: " + ZoneNamer.FriendlyZone(_currentLevel.Name));
            sb.AppendLine("Room: " + ZoneNamer.NormalizeRoomName(_currentLevel.Name));
            sb.AppendLine("Source: " + _currentLevel.SourceEntry.VirtualPath);
            sb.AppendLine("Version: 0x" + _currentLevel.Version.ToString("X8"));
            sb.AppendLine("Data start: 0x" + _currentLevel.DataStart.ToString("X"));
            sb.AppendLine("Terrain model pointer: 0x" + _currentLevel.ModelData.ToString("X"));
            sb.AppendLine("Vertex start: 0x" + _currentLevel.TerrainVertexStart.ToString("X"));
            sb.AppendLine("Face start: 0x" + _currentLevel.TerrainPolygonStart.ToString("X"));
            sb.AppendLine();
            sb.AppendLine("Object editing currently patches existing intro placement records in place.");
            sb.AppendLine("Add/remove needs table growth and pointer relocation, so it is deliberately not enabled yet.");
            AppendChangedObjectSummary(sb);
            if (_loadedLevels.Count > 1)
            {
                sb.AppendLine();
                foreach (SR1LevelDocument doc in _loadedLevels)
                {
                    sb.AppendLine(ZoneNamer.DisplayName(doc.Name) + " - " + doc.Summary);
                }
            }
            _levelSummary.Text = sb.ToString();
        }

        private void AppendChangedObjectSummary(StringBuilder sb)
        {
            int count = 0;
            foreach (SR1LevelDocument doc in _loadedLevels)
            {
                foreach (LevelObjectPlacement obj in doc.Objects)
                {
                    if (!obj.HasChanged) continue;
                    if (count == 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("Changed objects in working buffer:");
                    }

                    if (count < 40)
                    {
                        sb.Append(" - ");
                        sb.Append(ZoneNamer.NormalizeRoomName(doc.Name));
                        sb.Append(" / ");
                        sb.Append(ObjectNamer.DisplayName(obj.FileName));
                        sb.Append(" id ");
                        sb.Append(obj.UniqueId);
                        sb.Append(": ");
                        sb.Append(DescribeObjectChange(obj));
                        sb.AppendLine();
                    }
                    count++;
                }
            }

            if (count > 40)
            {
                sb.AppendLine(" - Additional changed objects omitted from this summary.");
            }
        }

        private static string DescribeObjectChange(LevelObjectPlacement obj)
        {
            List<string> parts = new List<string>();
            if (obj.HasMoved)
            {
                parts.Add(string.Format("move X {0}, Y {1}, Z {2}",
                    obj.X - obj.OriginalX,
                    obj.Y - obj.OriginalY,
                    obj.Z - obj.OriginalZ));
            }
            if (obj.HasRotated)
            {
                parts.Add(string.Format("rot X {0}, Y {1}, Z {2}",
                    obj.RotationRawX - obj.OriginalRotationRawX,
                    obj.RotationRawY - obj.OriginalRotationRawY,
                    obj.RotationRawZ - obj.OriginalRotationRawZ));
            }
            if (obj.HasSpectralChanged)
            {
                parts.Add(string.Format("spectral X {0}, Y {1}, Z {2}",
                    obj.SpectralX - obj.OriginalSpectralX,
                    obj.SpectralY - obj.OriginalSpectralY,
                    obj.SpectralZ - obj.OriginalSpectralZ));
            }
            return parts.Count == 0 ? "unchanged" : string.Join("; ", parts.ToArray());
        }

        private LevelObjectTag GetSelectedLevelObjectTag()
        {
            if (_levelObjects == null || _levelObjects.SelectedItems.Count == 0) return null;
            return _levelObjects.SelectedItems[0].Tag as LevelObjectTag;
        }

        private void RevertSelectedObjectValues()
        {
            LevelObjectTag tag = GetSelectedLevelObjectTag();
            if (tag == null)
            {
                MessageBox.Show(this, "Select an object first.", "Revert object", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            tag.Object.ResetToOriginal();
            SR1LevelParser.WriteObjectToRaw(tag.Document, tag.Object);
            LoadObjectIntoEditors(tag.Document, tag.Object);
            UpdateSelectedLevelObjectListItem(tag.Object);
            RefreshLevelSummaryText();
            if (_levelCanvas != null)
            {
                _levelCanvas.SelectObject(tag.Document, tag.Object.Index);
                _levelCanvas.Invalidate();
            }
            SetStatus("Reverted " + ObjectNamer.DisplayName(tag.Object.FileName) + " id " + tag.Object.UniqueId);
        }

        private void RevertAllObjectEdits()
        {
            if (_loadedLevels.Count == 0)
            {
                MessageBox.Show(this, "Load a level first.", "Revert objects", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int reverted = 0;
            foreach (SR1LevelDocument doc in _loadedLevels)
            {
                foreach (LevelObjectPlacement obj in doc.Objects)
                {
                    if (!obj.HasChanged) continue;
                    obj.ResetToOriginal();
                    SR1LevelParser.WriteObjectToRaw(doc, obj);
                    reverted++;
                }
            }

            if (_levelObjects.SelectedItems.Count > 0)
            {
                LevelObjectTag selected = _levelObjects.SelectedItems[0].Tag as LevelObjectTag;
                if (selected != null)
                {
                    LoadObjectIntoEditors(selected.Document, selected.Object);
                    UpdateSelectedLevelObjectListItem(selected.Object);
                }
            }

            PopulateLevelDocumentViews();
            SetStatus("Reverted " + reverted + " changed object(s)");
        }

        private byte[] BuildLevelReplacementBytes(SR1LevelDocument level, LevelObjectPlacement selectedOnly)
        {
            byte[] replacement = (byte[])level.RawEntryBytes.Clone();
            foreach (LevelObjectPlacement obj in level.Objects)
            {
                bool writeOriginal = selectedOnly != null && !object.ReferenceEquals(obj, selectedOnly);
                SR1LevelParser.WriteObjectToBytes(replacement, level.DataStart, obj, writeOriginal);
            }
            return replacement;
        }

        private void SaveSelectedObjectPatchAsCopy()
        {
            LevelObjectTag tag = GetSelectedLevelObjectTag();
            if (tag == null || tag.Document == null || tag.Object == null)
            {
                MessageBox.Show(this, "Select an object first.", "Level patch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!tag.Object.HasChanged)
            {
                MessageBox.Show(this, "The selected object has not been moved or rotated yet.", "Level patch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string warnings = BuildLevelPatchWarnings(tag.Document, tag.Object);
            if (!string.IsNullOrEmpty(warnings))
            {
                DialogResult result = MessageBox.Show(this,
                    warnings + Environment.NewLine + Environment.NewLine + "Create the selected-object BIN anyway?",
                    "Possible risky object move",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning);
                if (result != DialogResult.OK) return;
            }

            using (SaveFileDialog saveDlg = new SaveFileDialog())
            {
                string room = string.IsNullOrEmpty(tag.Document.Name) ? "level" : tag.Document.Name;
                string objCode = ObjectNamer.Normalize(tag.Object.FileName);
                saveDlg.Title = "Save selected object patched BIN copy";
                saveDlg.FileName = Path.GetFileNameWithoutExtension(_disc.ImagePath) + "." + room + "." + objCode + tag.Object.UniqueId + ".selected.patched.bin";
                saveDlg.Filter = "BIN images (*.bin)|*.bin|All files (*.*)|*.*";
                if (saveDlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    CopyImageWithProgress(_disc.ImagePath, saveDlg.FileName);
                    byte[] replacementBytes = BuildLevelReplacementBytes(tag.Document, tag.Object);
                    using (MemoryStream replacement = new MemoryStream(replacementBytes, false))
                    using (FileStream patchStream = File.Open(saveDlg.FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                    {
                        PatchReplacementBytes(tag.Document.SourceEntry, replacement, patchStream);
                    }
                    WriteCueForPatchedBin(saveDlg.FileName);
                    SetStatus("Selected-object patch written: " + saveDlg.FileName);
                    MessageBox.Show(this,
                        "Selected-object BIN copy created. Other edited objects in this room were restored to original values in the saved copy.",
                        "Level patch",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Level patch failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveCurrentLevelPatchAsCopy()
        {
            SR1LevelDocument targetLevel = _selectedLevelDocument ?? _currentLevel;
            if (targetLevel == null)
            {
                MessageBox.Show(this, "Load a level first.", "Level patch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string warnings = BuildLevelPatchWarnings(targetLevel);
            if (!string.IsNullOrEmpty(warnings))
            {
                DialogResult result = MessageBox.Show(this,
                    warnings + Environment.NewLine + Environment.NewLine + "Create the patched BIN anyway?",
                    "Possible risky object move",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning);
                if (result != DialogResult.OK) return;
            }

            using (SaveFileDialog saveDlg = new SaveFileDialog())
            {
                string room = string.IsNullOrEmpty(targetLevel.Name) ? "level" : targetLevel.Name;
                saveDlg.Title = "Save patched BIN copy";
                saveDlg.FileName = Path.GetFileNameWithoutExtension(_disc.ImagePath) + "." + room + ".patched.bin";
                saveDlg.Filter = "BIN images (*.bin)|*.bin|All files (*.*)|*.*";
                if (saveDlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    CopyImageWithProgress(_disc.ImagePath, saveDlg.FileName);
                    byte[] replacementBytes = BuildLevelReplacementBytes(targetLevel, null);
                    using (MemoryStream replacement = new MemoryStream(replacementBytes, false))
                    using (FileStream patchStream = File.Open(saveDlg.FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                    {
                        PatchReplacementBytes(targetLevel.SourceEntry, replacement, patchStream);
                    }
                    WriteCueForPatchedBin(saveDlg.FileName);
                    SetStatus("Patched level copy written: " + saveDlg.FileName);
                    MessageBox.Show(this, "Patched BIN copy created. A matching CUE was written beside it, and raw CD sector checksums were rebuilt.", "Level patch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Level patch failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string BuildLevelPatchWarnings(SR1LevelDocument level)
        {
            return BuildLevelPatchWarnings(level, null);
        }

        private string BuildLevelPatchWarnings(SR1LevelDocument level, LevelObjectPlacement selectedOnly)
        {
            if (level == null) return null;
            StringBuilder sb = new StringBuilder();
            int warningCount = 0;
            foreach (LevelObjectPlacement obj in level.Objects)
            {
                if (selectedOnly != null && !object.ReferenceEquals(obj, selectedOnly)) continue;
                if (!obj.HasMoved) continue;

                int dx = obj.X - obj.OriginalX;
                int dy = obj.Y - obj.OriginalY;
                int dz = obj.Z - obj.OriginalZ;
                int largest = Math.Max(Math.Abs(dx), Math.Max(Math.Abs(dy), Math.Abs(dz)));
                int radius = Math.Max(1, Math.Abs((int)obj.MaxRad));
                bool special = string.Equals(ObjectNamer.Normalize(obj.FileName), "splob", StringComparison.OrdinalIgnoreCase);
                string note = ObjectNamer.PlacementNote(obj.FileName);
                string moveSafety = BuildMoveSafetyNote(level, obj);
                bool largeMove = largest > Math.Max(1024, radius * 4);
                if (!special && !largeMove && string.IsNullOrEmpty(note) && string.IsNullOrEmpty(moveSafety)) continue;

                if (sb.Length == 0)
                {
                    sb.AppendLine("This patch includes object moves that may need more stream/BSP research:");
                }

                sb.Append(" - ");
                sb.Append(ObjectNamer.DisplayName(obj.FileName));
                sb.Append(" id ");
                sb.Append(obj.UniqueId);
                sb.Append(" moved by X ");
                sb.Append(dx);
                sb.Append(", Y ");
                sb.Append(dy);
                sb.Append(", Z ");
                sb.Append(dz);
                if (special) sb.Append(" (special spectral/soul effect)");
                if (largeMove) sb.Append(" (large jump vs radius " + radius + ")");
                sb.AppendLine();
                if (!string.IsNullOrEmpty(note))
                {
                    sb.Append("   ");
                    sb.AppendLine(note);
                }
                if (!string.IsNullOrEmpty(moveSafety))
                {
                    sb.Append("   ");
                    sb.AppendLine(moveSafety);
                }

                warningCount++;
                if (warningCount >= 6)
                {
                    sb.AppendLine(" - Additional moved objects omitted from this warning.");
                    break;
                }
            }
            return sb.Length == 0 ? null : sb.ToString();
        }

        private void AddIsoAudioCandidates(IsoFileEntry entry)
        {
            if (!entry.IsDirectory)
            {
                string ext = Path.GetExtension(entry.Name);
                if (ext.Equals(".XA", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".STR", StringComparison.OrdinalIgnoreCase))
                {
                    ListViewItem item = new ListViewItem(entry.FullPath);
                    item.SubItems.Add(ext.Equals(".XA", StringComparison.OrdinalIgnoreCase) ? "XA stream" : "STR movie stream");
                    item.SubItems.Add(Util.FormatSize(entry.Size));
                    item.SubItems.Add("LBA " + entry.Lba);
                    item.SubItems.Add("Outer disc file");
                    item.Tag = entry;
                    _audioCandidates.Items.Add(item);
                }
            }
            foreach (IsoFileEntry child in entry.Children)
            {
                AddIsoAudioCandidates(child);
            }
        }

        private static void AddResearchItem(ListView view, BigFileEntry entry)
        {
            ListViewItem item = new ListViewItem(entry.VirtualPath);
            item.SubItems.Add(entry.Kind);
            item.SubItems.Add(Util.FormatSize(entry.Size));
            item.SubItems.Add("0x" + entry.Offset.ToString("X8"));
            item.SubItems.Add(entry.Notes ?? "");
            item.Tag = entry;
            view.Items.Add(item);
        }

        private void ClearViews()
        {
            if (_details != null) _details.Clear();
            if (_hex != null) _hex.Clear();
            if (_strings != null) _strings.Clear();
            if (_list != null) _list.Items.Clear();
            if (_searchResults != null) _searchResults.Items.Clear();
        }

        private void ClearLevelEditor()
        {
            if (_levelSelector != null) _levelSelector.Items.Clear();
            if (_levelObjects != null) _levelObjects.Items.Clear();
            if (_levelPortals != null) _levelPortals.Items.Clear();
            if (_levelSummary != null) _levelSummary.Clear();
            if (_levelCanvas != null) _levelCanvas.Document = null;
            _currentLevel = null;
            _selectedLevelDocument = null;
            _loadedLevels.Clear();
        }

        private int CountBigFileEntries()
        {
            if (_bigFile == null) return 0;
            int count = 0;
            foreach (BigFileFolder folder in _bigFile.Folders) count += folder.Files.Count;
            return count;
        }

        private static IsoFileEntry FindIsoEntry(IsoFileEntry root, string fullPath)
        {
            if (root.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase)) return root;
            foreach (IsoFileEntry child in root.Children)
            {
                IsoFileEntry found = FindIsoEntry(child, fullPath);
                if (found != null) return found;
            }
            return null;
        }

        private void SelectTreeNodeForListItem()
        {
            if (_list.SelectedItems.Count == 0) return;
            object tag = _list.SelectedItems[0].Tag;
            TreeNode node = FindTreeNodeByTag(_tree.Nodes, tag);
            if (node != null)
            {
                _tree.SelectedNode = node;
                node.EnsureVisible();
            }
        }

        private static TreeNode FindTreeNodeByTag(TreeNodeCollection nodes, object tag)
        {
            foreach (TreeNode node in nodes)
            {
                if (object.ReferenceEquals(node.Tag, tag)) return node;
                TreeNode child = FindTreeNodeByTag(node.Nodes, tag);
                if (child != null) return child;
            }
            return null;
        }

        private void ExportSelected()
        {
            object tag = _tree.SelectedNode == null ? null : _tree.SelectedNode.Tag;
            Stream stream = null;
            string suggested = "resource.bin";
            try
            {
                IsoFileEntry iso = tag as IsoFileEntry;
                if (iso != null && !iso.IsDirectory)
                {
                    stream = _disc.OpenFile(iso);
                    suggested = Util.MakeSafeFileName(iso.Name);
                }

                BigFileEntry big = tag as BigFileEntry;
                if (big != null)
                {
                    stream = _bigFile.OpenFile(big);
                    suggested = string.Format("folder{0:X4}_file{1:X4}_{2}.bin", big.FolderIndex, big.FileIndex, Util.MakeSafeFileName(big.Kind ?? "unknown"));
                }

                if (stream == null)
                {
                    MessageBox.Show(this, "Select a file or BIGFILE entry first.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (stream)
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.FileName = suggested;
                    dlg.Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*";
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        using (FileStream outStream = File.Create(dlg.FileName))
                        {
                            CopyWithProgress(stream, outStream, stream.Length, "Exporting");
                        }
                        SetStatus("Exported " + dlg.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                if (stream != null) stream.Dispose();
                MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReplaceSelectedInNewCopy()
        {
            if (_disc == null)
            {
                MessageBox.Show(this, "Open an image first.", "Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            object tag = _tree.SelectedNode == null ? null : _tree.SelectedNode.Tag;
            long targetSize;
            string targetName;
            if (!TryGetReplacementTarget(tag, out targetSize, out targetName))
            {
                MessageBox.Show(this, "Select an ISO file or BIGFILE entry first. Directory replacement is not supported.", "Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (OpenFileDialog sourceDlg = new OpenFileDialog())
            {
                sourceDlg.Title = "Choose same-size replacement for " + targetName;
                sourceDlg.Filter = "All files (*.*)|*.*";
                if (sourceDlg.ShowDialog(this) != DialogResult.OK) return;

                long replacementSize = new FileInfo(sourceDlg.FileName).Length;
                if (replacementSize != targetSize)
                {
                    MessageBox.Show(this,
                        "Replacement must be exactly the same size for this first safe patcher." + Environment.NewLine +
                        "Selected resource: " + targetSize + " bytes" + Environment.NewLine +
                        "Replacement file: " + replacementSize + " bytes",
                        "Size mismatch",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                using (SaveFileDialog saveDlg = new SaveFileDialog())
                {
                    saveDlg.Title = "Save patched BIN copy";
                    string baseName = Path.GetFileNameWithoutExtension(_disc.ImagePath);
                    saveDlg.FileName = baseName + ".patched.bin";
                    saveDlg.Filter = "BIN images (*.bin)|*.bin|All files (*.*)|*.*";
                    if (saveDlg.ShowDialog(this) != DialogResult.OK) return;

                    try
                    {
                        CopyImageWithProgress(_disc.ImagePath, saveDlg.FileName);
                        using (FileStream patchStream = File.Open(saveDlg.FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                        using (FileStream replacement = File.OpenRead(sourceDlg.FileName))
                        {
                            PatchReplacementBytes(tag, replacement, patchStream);
                        }
                        WriteCueForPatchedBin(saveDlg.FileName);
                        SetStatus("Patched copy written: " + saveDlg.FileName);
                        MessageBox.Show(this, "Patched BIN copy created. A matching CUE was written beside it, and raw CD sector checksums were rebuilt.", "Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, "Replace failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExtractSelectedBigFolder()
        {
            BigFileFolder folder = _tree.SelectedNode == null ? null : _tree.SelectedNode.Tag as BigFileFolder;
            if (folder == null)
            {
                MessageBox.Show(this, "Select a BIGFILE folder first.", "Extract folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Choose extraction folder";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string target = Path.Combine(dlg.SelectedPath, "folder" + folder.Index.ToString("X4"));
                    Directory.CreateDirectory(target);
                    ExtractEntries(folder.Files, target);
                }
            }
        }

        private bool TryGetReplacementTarget(object tag, out long size, out string name)
        {
            IsoFileEntry iso = tag as IsoFileEntry;
            if (iso != null && !iso.IsDirectory)
            {
                size = iso.Size;
                name = iso.FullPath;
                return true;
            }

            BigFileEntry big = tag as BigFileEntry;
            if (big != null)
            {
                size = big.Size;
                name = big.VirtualPath;
                return true;
            }

            size = 0;
            name = null;
            return false;
        }

        private void PatchReplacementBytes(object tag, Stream replacement, FileStream patchedImage)
        {
            IsoFileEntry iso = tag as IsoFileEntry;
            BigFileEntry big = tag as BigFileEntry;

            uint startLba;
            long logicalOffset;
            long length;
            if (iso != null)
            {
                startLba = iso.Lba;
                logicalOffset = 0;
                length = iso.Size;
            }
            else if (big != null && _bigIsoEntry != null)
            {
                startLba = _bigIsoEntry.Lba;
                logicalOffset = big.Offset;
                length = big.Size;
            }
            else
            {
                throw new InvalidOperationException("Unsupported replacement target.");
            }

            byte[] buffer = new byte[128 * 1024];
            HashSet<uint> touchedSectors = new HashSet<uint>();
            long written = 0;
            _progress.Visible = true;
            try
            {
                while (written < length)
                {
                    int read = replacement.Read(buffer, 0, (int)Math.Min(buffer.Length, length - written));
                    if (read <= 0) throw new EndOfStreamException("Replacement ended early.");
                    int consumed = 0;
                    while (consumed < read)
                    {
                        long logical = logicalOffset + written + consumed;
                        uint sector = startLba + (uint)(logical / DiscImage.UserSectorSize);
                        int inSector = (int)(logical % DiscImage.UserSectorSize);
                        int take = Math.Min(read - consumed, DiscImage.UserSectorSize - inSector);
                        long rawOffset = (long)sector * _disc.RawSectorSize + _disc.UserDataOffset + inSector;
                        patchedImage.Position = rawOffset;
                        patchedImage.Write(buffer, consumed, take);
                        touchedSectors.Add(sector);
                        consumed += take;
                    }
                    written += read;
                    int percent = (int)Math.Min(100, written * 100 / length);
                    _progress.Value = percent;
                    SetStatus("Patching " + Util.FormatSize(written) + " / " + Util.FormatSize(length));
                    Application.DoEvents();
                }

                RecalculateTouchedSectors(patchedImage, touchedSectors);
            }
            finally
            {
                _progress.Visible = false;
            }
        }

        private void RecalculateTouchedSectors(FileStream patchedImage, HashSet<uint> touchedSectors)
        {
            if (_disc == null || _disc.RawSectorSize != 2352 || touchedSectors == null || touchedSectors.Count == 0) return;

            byte[] sectorBytes = new byte[2352];
            int done = 0;
            foreach (uint sector in touchedSectors)
            {
                long rawOffset = (long)sector * _disc.RawSectorSize;
                patchedImage.Position = rawOffset;
                int total = 0;
                while (total < sectorBytes.Length)
                {
                    int read = patchedImage.Read(sectorBytes, total, sectorBytes.Length - total);
                    if (read <= 0) throw new EndOfStreamException("Patched sector ended early.");
                    total += read;
                }

                CdSectorChecksums.Recalculate(sectorBytes, _disc.UserDataOffset);

                patchedImage.Position = rawOffset;
                patchedImage.Write(sectorBytes, 0, sectorBytes.Length);

                done++;
                if (done % 32 == 0 || done == touchedSectors.Count)
                {
                    int percent = (int)Math.Min(100, done * 100L / touchedSectors.Count);
                    _progress.Value = percent;
                    SetStatus("Rebuilding CD sector checksums " + done + " / " + touchedSectors.Count);
                    Application.DoEvents();
                }
            }
        }

        private void CopyImageWithProgress(string sourcePath, string targetPath)
        {
            using (FileStream input = File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (FileStream output = File.Create(targetPath))
            {
                CopyWithProgress(input, output, input.Length, "Copying image");
            }
        }

        private void WriteCueForPatchedBin(string binPath)
        {
            if (_disc.RawSectorSize != 2352) return;
            string cuePath = Path.ChangeExtension(binPath, ".cue");
            string fileName = Path.GetFileName(binPath).Replace("\"", "");
            string cue = "FILE \"" + fileName + "\" BINARY" + Environment.NewLine +
                         "  TRACK 01 MODE2/2352" + Environment.NewLine +
                         "    INDEX 01 00:00:00" + Environment.NewLine;
            File.WriteAllText(cuePath, cue, Encoding.ASCII);
        }

        private void ExtractAllBigFile()
        {
            if (_bigFile == null)
            {
                MessageBox.Show(this, "Open a Soul Reaver image with BIGFILE.DAT first.", "Extract BIGFILE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Choose extraction folder";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    foreach (BigFileFolder folder in _bigFile.Folders)
                    {
                        string target = Path.Combine(dlg.SelectedPath, "folder" + folder.Index.ToString("X4"));
                        Directory.CreateDirectory(target);
                        ExtractEntries(folder.Files, target);
                    }
                    SetStatus("Extracted BIGFILE.DAT");
                }
            }
        }

        private void ExtractEntries(IEnumerable<BigFileEntry> entries, string targetFolder)
        {
            foreach (BigFileEntry entry in entries)
            {
                string name = string.Format("file{0:X4}_{1}_{2:X8}_{3:X8}.bin", entry.FileIndex, Util.MakeSafeFileName(entry.Kind ?? "Unknown"), entry.Hash1, entry.Hash2);
                string path = Path.Combine(targetFolder, name);
                using (Stream input = _bigFile.OpenFile(entry))
                using (FileStream output = File.Create(path))
                {
                    CopyWithProgress(input, output, input.Length, "Extracting " + name);
                }
                Application.DoEvents();
            }
        }

        private void RunSearch()
        {
            try
            {
                byte[] needle = _searchMode.SelectedIndex == 0 ? Encoding.ASCII.GetBytes(_searchText.Text) : Util.ParseHexPattern(_searchText.Text);
                if (needle.Length == 0) return;

                _searchResults.Items.Clear();
                if (_searchScope.SelectedIndex == 0)
                {
                    object tag = _tree.SelectedNode == null ? null : _tree.SelectedNode.Tag;
                    using (Stream s = OpenStreamForTag(tag))
                    {
                        if (s == null)
                        {
                            MessageBox.Show(this, "Select a file or BIGFILE entry first.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        SearchStream(s, SelectedLocationName(tag), needle, 200);
                    }
                }
                else
                {
                    if (_bigFile == null) return;
                    foreach (BigFileEntry entry in _bigFile.AllFiles())
                    {
                        using (Stream s = _bigFile.OpenFile(entry))
                        {
                            SearchStream(s, entry.VirtualPath, needle, 20);
                        }
                        if (_searchResults.Items.Count >= 500) break;
                        Application.DoEvents();
                    }
                }
                SetStatus("Search complete: " + _searchResults.Items.Count + " hit(s)");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Search failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SearchStream(Stream stream, string location, byte[] needle, int maxHitsForStream)
        {
            const int chunkSize = 64 * 1024;
            byte[] buffer = new byte[chunkSize + 256];
            int overlap = Math.Max(0, needle.Length - 1);
            long baseOffset = 0;
            int carry = 0;
            int hits = 0;
            stream.Position = 0;

            while (true)
            {
                int read = stream.Read(buffer, carry, chunkSize);
                if (read <= 0) break;
                int total = carry + read;
                for (int i = 0; i <= total - needle.Length; i++)
                {
                    bool match = true;
                    for (int j = 0; j < needle.Length; j++)
                    {
                        if (buffer[i + j] != needle[j])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        long hitOffset = baseOffset + i - carry;
                        AddSearchHit(location, hitOffset, "Pattern match");
                        hits++;
                        if (hits >= maxHitsForStream || _searchResults.Items.Count >= 500) return;
                    }
                }
                carry = Math.Min(overlap, total);
                if (carry > 0) Array.Copy(buffer, total - carry, buffer, 0, carry);
                baseOffset += read;
            }
        }

        private void AddSearchHit(string location, long offset, string description)
        {
            ListViewItem item = new ListViewItem(location);
            item.SubItems.Add("0x" + offset.ToString("X"));
            item.SubItems.Add(description);
            _searchResults.Items.Add(item);
        }

        private void ScanSelectedForSignatures()
        {
            object tag = _tree.SelectedNode == null ? null : _tree.SelectedNode.Tag;
            using (Stream s = OpenStreamForTag(tag))
            {
                if (s == null)
                {
                    MessageBox.Show(this, "Select a file or BIGFILE entry first.", "Signature scan", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _searchResults.Items.Clear();
                int max = (int)Math.Min(s.Length, 16 * 1024 * 1024);
                List<SignatureHit> hits = SignatureScanner.Scan(s, max);
                foreach (SignatureHit hit in hits)
                {
                    AddSearchHit(SelectedLocationName(tag), hit.Offset, hit.Kind + " - " + hit.Detail);
                }
                SetStatus("Signature scan complete: " + hits.Count + " hit(s)");
            }
        }

        private Stream OpenStreamForTag(object tag)
        {
            IsoFileEntry iso = tag as IsoFileEntry;
            if (iso != null && !iso.IsDirectory) return _disc.OpenFile(iso);
            BigFileEntry big = tag as BigFileEntry;
            if (big != null) return _bigFile.OpenFile(big);
            return null;
        }

        private string SelectedLocationName(object tag)
        {
            IsoFileEntry iso = tag as IsoFileEntry;
            if (iso != null) return iso.FullPath;
            BigFileEntry big = tag as BigFileEntry;
            if (big != null) return big.VirtualPath;
            return "(selected)";
        }

        private void CopyWithProgress(Stream input, Stream output, long total, string label)
        {
            byte[] buffer = new byte[128 * 1024];
            long copied = 0;
            _progress.Visible = true;
            try
            {
                while (true)
                {
                    int read = input.Read(buffer, 0, buffer.Length);
                    if (read <= 0) break;
                    output.Write(buffer, 0, read);
                    copied += read;
                    if (total > 0)
                    {
                        int percent = (int)Math.Min(100, copied * 100 / total);
                        _progress.Value = percent;
                    }
                    SetStatus(label + " " + Util.FormatSize(copied) + " / " + Util.FormatSize(total));
                    Application.DoEvents();
                }
            }
            finally
            {
                _progress.Visible = false;
            }
        }

        private void SetStatus(string text)
        {
            _status.Text = text;
            _status.Owner.Refresh();
        }

        private sealed class LevelComboItem
        {
            public readonly string Text;
            public readonly BigFileEntry Entry;

            public LevelComboItem(string text, BigFileEntry entry)
            {
                Text = text;
                Entry = entry;
            }

            public override string ToString()
            {
                return Text;
            }
        }

        private sealed class LevelObjectTag
        {
            public readonly SR1LevelDocument Document;
            public readonly LevelObjectPlacement Object;

            public LevelObjectTag(SR1LevelDocument document, LevelObjectPlacement obj)
            {
                Document = document;
                Object = obj;
            }
        }
    }
}
