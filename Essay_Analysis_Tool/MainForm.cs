using Essay_Analysis_Tool.Properties;
using Essay_Analysis_Tool.Utils;
using Essay_Analysis_Tool.Windows;
using FastColoredTextBoxNS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Essay_Analysis_Tool
{
    /// <summary>
    /// Defines the <see cref="MainForm" />
    /// </summary>
    public partial class MainForm : Form
    {
        #region Variable declarations and definitions

        //Dialog definitions
        internal OpenFileDialog file_open;
        internal FontDialog fontDialog;
        internal LoggerForm logger;
        internal DocMap documentMap;

        //General variable declarations and definitions
        private readonly Range _selection;
        private bool _highlightCurrentLine = true;
        private bool _enableDocumentMap = true;
        private Font font = new Font("Consolas", 9.75f);

        /// <summary>
        /// Defines the Platform Type.
        /// </summary>
        protected static readonly Platform platformType = PlatformType.GetOperationSystemPlatform();

        //Styles
        private Style sameWordsStyle = new MarkerStyle(new SolidBrush(Color.FromArgb(50, Color.Gray)));

        /// <summary>
        /// Gets the Current instance of <see cref="Editor"/>
        /// </summary>
        public Editor CurrentTB
        {
            get
            {
                Form activeMdi = ActiveMdiChild;

                if (activeMdi != null && activeMdi is Editor)
                {
                    return activeMdi as Editor;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets or sets Current selection <see cref="Range"/>.
        /// </summary>
        public Range Selection
        {
            get { return _selection; }
            set
            {
                if (value == _selection)
                {
                    return;
                }

                _selection.BeginUpdate();
                _selection.Start = value.Start;
                _selection.End = value.End;
                _selection.EndUpdate();
                Invalidate();
            }
        }

        AutocompleteMenu popupMenu;
        string[] keywords = { "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while", "add", "alias", "ascending", "descending", "dynamic", "from", "get", "global", "group", "into", "join", "let", "orderby", "partial", "remove", "select", "set", "value", "var", "where", "yield" };
        string[] methods = { "Equals()", "GetHashCode()", "GetType()", "ToString()" };
        string[] snippets = { "if(^)\n{\n;\n}", "if(^)\n{\n;\n}\nelse\n{\n;\n}", "for(^;;)\n{\n;\n}", "while(^)\n{\n;\n}", "do${\n^;\n}while();", "switch(^)\n{\ncase : break;\n}" };
        string[] declarationSnippets = {
               "public class ^\n{\n}", "private class ^\n{\n}", "internal class ^\n{\n}",
               "public struct ^\n{\n;\n}", "private struct ^\n{\n;\n}", "internal struct ^\n{\n;\n}",
               "public void ^()\n{\n;\n}", "private void ^()\n{\n;\n}", "internal void ^()\n{\n;\n}", "protected void ^()\n{\n;\n}",
               "public ^{ get; set; }", "private ^{ get; set; }", "internal ^{ get; set; }", "protected ^{ get; set; }"
               };

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            logger = new LoggerForm();

            dockpanel.Theme = new VS2015LightTheme();
            IsMdiContainer = true;

            logger.Log("Form Initialized!", LoggerMessageType.Info);
            
            CreateTab(null);
            UpdateDocumentMap();

            BuildAutocompleteMenu();
        }

        #region Tab Functionality

        List<Editor> tablist = new List<Editor>();

        private void CreateTab(string fileName)
        {
            var tab = new Editor(this, fileName, font);
            tab.Tag = fileName;

            if (fileName != null && !IsFileAlreadyOpen(fileName))
            {
                tab.SetCurrentEditorSyntaxHighlight(fileName);
                tab.OpenFile(fileName);
            }
            else if (fileName != null)
            {
                return;
            }

            tab.Title = fileName != null ? Path.GetFileName(fileName) : "new " + tablist.Count;

            tab.mainEditor.Focus();
            tab.mainEditor.KeyDown += new KeyEventHandler(MainForm_KeyDown);
            tab.mainEditor.TextChangedDelayed += new EventHandler<TextChangedEventArgs>(Tb_TextChangedDelayed);
            tab.Show(this.dockpanel, DockState.Document);
            tablist.Add(tab);
            UpdateDocumentMap();
            HighlightCurrentLine();
        }

        private bool IsFileAlreadyOpen(string fileName)
        {
            foreach (Editor tab in tablist)
            {
                if (tab.Tag as string == fileName)
                {
                    this.ActivateMdiChild(tab);
                    return true;
                }
            }
            return false;
        }

        private void CloseAllTabs()
        {
            foreach (Editor tab in tablist.ToArray())
                tab.Close();
        }

        private bool CallSave(Editor tab)
        {
            if (tab.Save())
            {
                UpdateChangedFlag(false);
                return true;
            }
            return false;
        }

        private void HighlightCurrentLine()
        {
            foreach (Editor tab in tablist.ToArray())
            {
                tab.CurrentLineHighlight = tab.CurrentLineHighlight ? false : true;
            }
            if (CurrentTB != null)
            {
                CurrentTB.Invalidate();
            }
        }

        private void ChangeFont(Font font)
        {
            if (font.Size <= 4)
                return;

            this.font = font;
            foreach (Editor tab in tablist)
                tab.mainEditor.Font = font;
        }

        #endregion

        #region Button Click Event Handlers

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
            {
                CurrentTB.mainEditor.Undo();
            }
        }

        private void NewToolStripButton_Click(object sender, EventArgs e)
        {
            CreateTab(null);
        }

        private void FindButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
            {
                CurrentTB.mainEditor.ShowFindDialog();
            }
        }

        private void OpenToolStripButton_Click(object sender, EventArgs e)
        {
            file_open = new OpenFileDialog();
            if (file_open.ShowDialog() == DialogResult.OK)
                CreateTab(file_open.FileName);
        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            OpenFileDialog fd = (OpenFileDialog) sender;
            string fileName = fd.FileName;

            CreateTab(fileName);
        }

        private void CloseToolStripButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.Close();
        }

        public void CloseTab(Editor tab)
        {
            if (CurrentTB != null)
            {
                EditorClosingEventArgs args = new EditorClosingEventArgs(tab);
                Editor_TabClosing(args);
                if (args.Cancel)
                {
                    return;
                }
                tablist.Remove(tab);
                dockpanel.Controls.Remove(tab);
                UpdateDocumentMap();
            }
        }

        private void FontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fontDialog == null)
            {
                fontDialog = new FontDialog();
                fontDialog.Font = font;
            }
            if (CurrentTB != null && fontDialog.ShowDialog() == DialogResult.OK)
            {
                ChangeFont(fontDialog.Font);
                font = fontDialog.Font;
            }
        }

        private void SaveToolStripButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CallSave(CurrentTB);
        }

        private void CloseAllToolStripButton_Click(object sender, EventArgs e)
        {
            CloseAllTabs();
        }

        private void CutToolStripButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.mainEditor.Cut();
        }

        private void PasteToolStripButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.mainEditor.Paste();
        }

        private void CopyToolStripButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.mainEditor.Copy();
        }

        private void UndoToolStripButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.mainEditor.Undo();
        }

        private void RedoToolStripButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.mainEditor.Redo();
        }

        private void ZoomInToolStripButton_Click(object sender, EventArgs e)
        {
            ChangeFont(new Font(font.Name, font.Size + 2));
        }

        private void ZoomOutToolStripButton_Click(object sender, EventArgs e)
        {
            ChangeFont(new Font(font.Name, font.Size - 2));
        }

        private void FindToolStripButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.mainEditor.ShowFindDialog();
        }

        private void DocumentMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _enableDocumentMap = !_enableDocumentMap;
            UpdateDocumentMap();
        }

        private void CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            syntaxLabel.Text = "C#";

            if (CurrentTB != null)
                CurrentTB.ChangeSyntax(Language.CSharp);
        }

        private void NoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            syntaxLabel.Text = "None";

            if (CurrentTB != null)
                CurrentTB.ChangeSyntax(Language.Custom);
        }

        private void HTMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            syntaxLabel.Text = "HTML";

            if (CurrentTB != null)
                CurrentTB.ChangeSyntax(Language.HTML);
        }

        private void JavaScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            syntaxLabel.Text = "JavaScript";

            if (CurrentTB != null)
                CurrentTB.ChangeSyntax(Language.JS);
        }

        private void LuaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            syntaxLabel.Text = "Lua";

            if (CurrentTB != null)
                CurrentTB.ChangeSyntax(Language.Lua);
        }

        private void PHPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            syntaxLabel.Text = "PHP";

            if (CurrentTB != null)
                CurrentTB.ChangeSyntax(Language.PHP);
        }

        private void SQLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            syntaxLabel.Text = "SQL";

            if (CurrentTB != null)
                CurrentTB.ChangeSyntax(Language.SQL);
        }

        private void VisualBasicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            syntaxLabel.Text = "Visual Basic";

            if (CurrentTB != null)
                CurrentTB.ChangeSyntax(Language.VB);
        }

        private void XMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            syntaxLabel.Text = "XML";

            if (CurrentTB != null)
                CurrentTB.ChangeSyntax(Language.XML);
        }

        private void StatusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (statusBarToolStripMenuItem.Checked)
                statusStrip1.Show();
            else
                statusStrip1.Hide();
        }

        private void RefreshToolStripButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
            {
                CurrentTB.Refresh();
                var r = new Range(CurrentTB.mainEditor);
                r.SelectAll();
                CurrentTB.mainEditor.OnSyntaxHighlight(new TextChangedEventArgs(r));
            }
        }

        private void HlCurrentLineToolStripButton_Click(object sender, EventArgs e)
        {
            _highlightCurrentLine = _highlightCurrentLine ? false : true;

            HighlightCurrentLine();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            file_open = new OpenFileDialog();
            if (file_open.ShowDialog() == DialogResult.OK)
                CreateTab(file_open.FileName);
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CallSave(CurrentTB);
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
            {
                string oldFile = CurrentTB.Tag as string;
                CurrentTB.Tag = null;
                if (!CallSave(CurrentTB))
                {
                    if (oldFile != null)
                    {
                        CurrentTB.Tag = oldFile;
                        CurrentTB.Title = Path.GetFileName(oldFile);
                    }
                }
            }
        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateTab(null);
        }

        private void ReplaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.mainEditor.ShowReplaceDialog();
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.mainEditor.Copy();
        }

        private void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.mainEditor.Redo();
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.mainEditor.Cut();
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.mainEditor.Paste();
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string title = "About Notepad#";
            string message = "Created by: Zachary Pedigo\nVersion: " + Resources.ApplicationVersion + "\n" + "Date: " + DateTime.Now + "\n" + "OS: "
                + Environment.OSVersion + "\nLicense: GNU General Public License v3.0";
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoggerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logger.Show();
        }

        #endregion

        #region Event Handlers

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (CurrentTB != null)
            {
                if (e.Control && e.KeyCode == Keys.O)
                {
                    file_open = new OpenFileDialog();
                    if (file_open.ShowDialog() == DialogResult.OK)
                        CreateTab(file_open.FileName);
                }
                else if (e.KeyCode == Keys.S && e.Modifiers == Keys.Control)
                {
                    if (CurrentTB != null)
                        CallSave(CurrentTB);
                }
                else if (e.Control && e.Shift && e.KeyCode == Keys.L)
                    CurrentTB.ClearCurrentLine();
                else if (e.Control && e.Shift && e.KeyCode == Keys.Oem2 && CurrentTB.mainEditor.CommentPrefix != null)
                {
                    if (!CurrentTB.mainEditor.SelectedText.Contains(CurrentTB.mainEditor.CommentPrefix))
                        CurrentTB.mainEditor.InsertLinePrefix(CurrentTB.mainEditor.CommentPrefix);
                    else
                        CurrentTB.mainEditor.RemoveLinePrefix(CurrentTB.mainEditor.CommentPrefix);
                }
            }
        }

        private void Tb_TextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            var tb = sender as FastColoredTextBox;
            UpdateChangedFlag(tb.IsChanged);
        }

        private void TsFiles_TabStripItemClosed(object sender, EventArgs e)
        {
            UpdateDocumentMap();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (Editor tab in tablist.ToArray())
            {
                EditorClosingEventArgs args = new EditorClosingEventArgs(tab);
                Editor_TabClosing(args);
                if (args.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                tablist.Remove(tab);
                tab.Close();
            }
        }

        private void Editor_TabClosing(EditorClosingEventArgs e)
        {
            if (e.Item.mainEditor.IsChanged)
            {
                switch (MessageBox.Show("Do you want save " + e.Item.Title + " ?", "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information))
                {
                    case DialogResult.Yes:
                        if (!CallSave(e.Item))
                            e.Cancel = true;
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void dockpanel_ActiveContentChanged(object sender, EventArgs e)
        {
            UpdateDocumentMap();

            if (CurrentTB != null)
            {
                BuildAutocompleteMenu();
                UpdateChangedFlag(CurrentTB.mainEditor.IsChanged);
            }
        }

        private bool IsSavedTab(string tagName) => tagName != null;

        private void DiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> filePaths = new List<string>();

            foreach (Editor item in tablist)
            {
                string filePath = (string)item.Tag;

                if (IsSavedTab(filePath))
                    filePaths.Add(filePath);

                    if (filePaths.Count == 2)
                        break;
            }

            if  (filePaths.Count == 1)
                new DiffViewerForm(filePaths[0]).Show();
            else if (filePaths.Count == 2)
                new DiffViewerForm(filePaths[0], filePaths[1]).Show();
            else
                new DiffViewerForm().Show();
        }

        #endregion

        #region Document Map Functionality

        private void UpdateDocumentMap()
        {
            if (documentMap == null && _enableDocumentMap)
                BuildDocumentMap();

            if (CurrentTB != null && documentMap != null)
            {
                documentMap.Target = tablist.Count > 0 ? CurrentTB.mainEditor : null;
                documentMap.Visible = _enableDocumentMap;
                if (!_enableDocumentMap || documentMap.Target == null)
                {
                    documentMap.Close();
                    documentMap = null;
                    return;
                }
            }
        }

        private void BuildDocumentMap()
        {
            documentMap = new DocMap();
            documentMap.Show(this.dockpanel, DockState.DockRight);
        }

        #endregion

        #region AutoCompleteMenu Functionality

        private void BuildAutocompleteMenu()
        {
            if (CurrentTB == null)
                return;
            List<AutocompleteItem> items = new List<AutocompleteItem>();

            foreach (var item in snippets)
                items.Add(new SnippetAutocompleteItem(item) { ImageIndex = 1 });
            foreach (var item in declarationSnippets)
                items.Add(new DeclarationSnippet(item) { ImageIndex = 0 });
            foreach (var item in methods)
                items.Add(new MethodAutocompleteItem(item) { ImageIndex = 2 });
            foreach (var item in keywords)
                items.Add(new AutocompleteItem(item));

            items.Add(new InsertSpaceSnippet());
            items.Add(new InsertSpaceSnippet(@"^(\w+)([=<>!:]+)(\w+)$"));
            items.Add(new InsertEnterSnippet());

            popupMenu = new AutocompleteMenu(CurrentTB.mainEditor);

            //set as autocomplete source
            popupMenu.Items.SetAutocompleteItems(items);
        }

        /// <summary>
        /// This item appears when any part of snippet text is typed
        /// </summary>
        class DeclarationSnippet : SnippetAutocompleteItem
        {
            public DeclarationSnippet(string snippet)
                : base(snippet)
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                var pattern = Regex.Escape(fragmentText);
                if (Regex.IsMatch(Text, "\\b" + pattern, RegexOptions.IgnoreCase))
                    return CompareResult.Visible;
                return CompareResult.Hidden;
            }
        }

        /// <summary>
        /// Divides numbers and words: "123AND456" -> "123 AND 456"
        /// Or "i=2" -> "i = 2"
        /// </summary>
        class InsertSpaceSnippet : AutocompleteItem
        {
            string pattern;

            public InsertSpaceSnippet(string pattern) : base("")
            {
                this.pattern = pattern;
            }

            public InsertSpaceSnippet()
                : this(@"^(\d+)([a-zA-Z_]+)(\d*)$")
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                if (Regex.IsMatch(fragmentText, pattern))
                {
                    Text = InsertSpaces(fragmentText);
                    if (Text != fragmentText)
                        return CompareResult.Visible;
                }
                return CompareResult.Hidden;
            }

            public string InsertSpaces(string fragment)
            {
                var m = Regex.Match(fragment, pattern);
                if (m == null)
                    return fragment;
                if (m.Groups[1].Value == "" && m.Groups[3].Value == "")
                    return fragment;
                return (m.Groups[1].Value + " " + m.Groups[2].Value + " " + m.Groups[3].Value).Trim();
            }

            public override string ToolTipTitle
            {
                get
                {
                    return Text;
                }
            }
        }

        /// <summary>
        /// Inerts line break after '}'
        /// </summary>
        class InsertEnterSnippet : AutocompleteItem
        {
            Place enterPlace = Place.Empty;

            public InsertEnterSnippet()
                : base("[Line break]")
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                var r = Parent.Fragment.Clone();
                while (r.Start.iChar > 0)
                {
                    if (r.CharBeforeStart == '}')
                    {
                        enterPlace = r.Start;
                        return CompareResult.Visible;
                    }

                    r.GoLeftThroughFolded();
                }

                return CompareResult.Hidden;
            }

            public override string GetTextForReplace()
            {
                //extend range
                Range r = Parent.Fragment;
                Place end = r.End;
                r.Start = enterPlace;
                r.End = r.End;
                //insert line break
                return Environment.NewLine + r.Text;
            }

            public override void OnSelected(AutocompleteMenu popupMenu, SelectedEventArgs e)
            {
                base.OnSelected(popupMenu, e);
                if (Parent.Fragment.tb.AutoIndent)
                    Parent.Fragment.tb.DoAutoIndent();
            }

            public override string ToolTipTitle
            {
                get
                {
                    return "Insert line break after '}'";
                }
            }
        }

        #endregion

        #region Tool Strip Functionality

        private void UpdateChangedFlag(bool isChanged)
        {
            if (CurrentTB != null)
            {
                CurrentTB.mainEditor.IsChanged = isChanged;
                saveToolStripButton.Enabled = isChanged;
            }
        }

        #endregion
    }
}
