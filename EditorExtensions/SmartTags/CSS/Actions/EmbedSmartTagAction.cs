﻿using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace MadsKristensen.EditorExtensions
{
    internal class EmbedSmartTagAction : CssSmartTagActionBase
    {
        private readonly ITrackingSpan _span;
        private readonly UrlItem _url;

        public EmbedSmartTagAction(ITrackingSpan span, UrlItem url)
        {
            _span = span;
            _url = url;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2012;component/Resources/embed.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return Resources.UrlSmartTagActionName; }
        }

        public override void Invoke()
        {
            string selection = _url.UrlString.Text;

            if (selection != null)
            {
                string filePath = ProjectHelpers.ToAbsoluteFilePath(selection);
                ApplyChanges(filePath);
            }
        }

        private void ApplyChanges(string filePath)
        {
            ITextSnapshot snapshot = _span.TextBuffer.CurrentSnapshot;

            if (File.Exists(filePath))
            {
                string dataUri = "url('" + FileHelpers.ConvertToBase64(filePath) + "') /*" + _url.UrlString.Text.Trim('"', '\'') + "*/";
                InsertEmbedString(snapshot, dataUri);
            }
            else
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.CheckFileExists = true;
                    dialog.Multiselect = false;
                    dialog.InitialDirectory = new FileInfo(EditorExtensionsPackage.DTE.ActiveDocument.FullName).Directory.FullName;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string dataUri = "url('" + FileHelpers.ConvertToBase64(dialog.FileName) + "')";
                        InsertEmbedString(snapshot, dataUri);
                    }
                }
            }
        }

        private void InsertEmbedString(ITextSnapshot snapshot, string dataUri)
        {
            EditorExtensionsPackage.DTE.UndoContext.Open(DisplayText);
            Declaration dec = _url.FindType<Declaration>();
            
            if (dec != null && dec.Parent != null && !(dec.Parent.Parent is FontFaceDirective)) // No declartion in LESS variable definitions
            {
                RuleBlock rule = _url.FindType<RuleBlock>();
                string text = dec.Text;

                if (rule != null)
                {
                    Declaration match = rule.Declarations.FirstOrDefault(d => d.PropertyName != null && d.PropertyName.Text == "*" + dec.PropertyName.Text);
                    if (!text.StartsWith("*") && match == null)
                        _span.TextBuffer.Insert(dec.AfterEnd, "*" + text + "/* For IE 6 and 7 */");
                }
            }

            _span.TextBuffer.Replace(_span.GetSpan(snapshot), dataUri);

            EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");
            EditorExtensionsPackage.ExecuteCommand("Edit.CollapsetoDefinitions");
            EditorExtensionsPackage.DTE.UndoContext.Close();
        }
    }
}
