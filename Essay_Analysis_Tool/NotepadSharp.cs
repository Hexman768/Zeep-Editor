using Essay_Analysis_Tool.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Essay_Analysis_Tool
{
    /// <summary>
    /// This static class represents the Notepad Sharp application itself.
    /// </summary>
    public static class NotepadSharp
    {
        static MainForm MainForm { get; set; }
        static List<Editor> tablist = new List<Editor>();

        /// <summary>
        /// Creates a new instance of <see cref="Editor"/> with the given filename.
        /// If no filename is given then a blank instance is created and returned.
        /// </summary>
        /// <param name="fileName">File Name</param>
        /// <returns>A new instance of <see cref="Editor"/></returns>
        public static Editor CreateTab(String fileName)
        {
            var tab = new Editor(MainForm, fileName, new System.Drawing.Font("Consolas", 9.75f));
            tab.Tag = fileName;

            if (fileName != null && !IsFileAlreadyOpen(fileName))
            {
                tab.SetCurrentEditorSyntaxHighlight(fileName);
                tab.OpenFile(fileName);
            }
            else if (fileName != null)
            {
                return null;
            }

            tab.Title = fileName != null ? Path.GetFileName(fileName) : "new " + tablist.Count;

            tablist.Add(tab);
            HighlightCurrentLine();

            return tab;
        }

        /// <summary>
        /// Closes all <see cref="Editor"/> instances in the list.
        /// </summary>
        public static void CloseAllTabs()
        {
            foreach (Editor tab in tablist.ToArray())
                tab.Close();
        }

        /// <summary>
        /// Enables or disables current line highlighting depending on the value
        /// of a boolean variable.
        /// </summary>
        public static void HighlightCurrentLine()
        {
            foreach (Editor tab in tablist.ToArray())
            {
                tab.CurrentLineHighlight = tab.CurrentLineHighlight ? false : true;
            }
            if (MainForm.CurrentTB != null)
            {
                MainForm.CurrentTB.Invalidate();
            }
        }

        private static bool IsFileAlreadyOpen(string fileName)
        {
            foreach (Editor tab in tablist)
            {
                if (tab.Tag as string == fileName)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
