﻿using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Editors;
using SerialLoops.Lib.Script;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Controls
{
    public class ScriptCommandListPanel : Panel
    {
        public Dictionary<ScriptSection, List<ScriptItemCommand>> Commands
        {
            protected get { return _commands; }
            set
            {
                _commands = value;
                Viewer?.SetContents(GetSections(), _expandItems);
            }
        }
        public ScriptCommandSectionTreeGridView Viewer { get; private set; }

        protected ILogger _log;
        private readonly Size _size;
        private Dictionary<ScriptSection, List<ScriptItemCommand>> _commands;
        private readonly bool _expandItems;
        private readonly Editor _editor;

        public ScriptCommandListPanel(Dictionary<ScriptSection, List<ScriptItemCommand>> commands, Size size, bool expandItems, Editor editor, ILogger log)
        {
            Commands = commands;
            _log = log;
            _size = size;
            _expandItems = expandItems;
            _editor = editor;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            Viewer = new ScriptCommandSectionTreeGridView(GetSections(), _editor, _size, _expandItems, _log);
            MinimumSize = _size;
            Padding = 0;
            Content = new TableLayout(Viewer.Control);

            Viewer.RepositionCommand += (o, e) =>
            {
                var treeGridView = (ScriptCommandSectionTreeGridView)o;
                var entry = (ScriptCommandSectionEntry)treeGridView.SelectedItem;
                var args = (ScriptCommandSectionEventArgs)e;

                if (args.NewParent.Text == "Top")
                {
                    ScriptSection section = entry.ScriptFile.ScriptSections.First(s => s.Name.Replace("/", "") == entry.Text);
                    entry.ScriptFile.ScriptSections.Remove(section);
                    entry.ScriptFile.ScriptSections.Insert(args.NewIndex, section);

                    LabelsSectionEntry label = entry.ScriptFile.LabelsSection.Objects.FirstOrDefault(l => l.Name.Replace("/", "") == entry.Text);
                    if (label is not null)
                    {
                        entry.ScriptFile.LabelsSection.Objects.Remove(label);
                        entry.ScriptFile.LabelsSection.Objects.Insert(args.NewIndex, label);
                    }
                }
                else
                {
                    ScriptCommandInvocation command = entry.Command.Script.ScriptSections[entry.Command.Script.ScriptSections.IndexOf(entry.Command.Section)]
                        .Objects[entry.Command.Index];
                    entry.Command.Script.ScriptSections[entry.Command.Script.ScriptSections.IndexOf(entry.Command.Section)]
                        .Objects.RemoveAt(entry.Command.Index);
                    _commands[entry.Command.Section].RemoveAt(entry.Command.Index);
                    for (int i = entry.Command.Index; i < _commands[entry.Command.Section].Count; i++)
                    {
                        _commands[entry.Command.Section][i].Index--;
                    }
                    entry.Command.Section = entry.Command.Script.ScriptSections.First(s => s.Name.Replace("/", "").Equals(args.NewParent.Text));
                    entry.Command.Section.Objects.Insert(args.NewIndex, command);
                    entry.Command.Index = args.NewIndex;
                    _commands[entry.Command.Section].Insert(args.NewIndex, entry.Command);
                    for (int i = args.NewIndex + 1; i < _commands[entry.Command.Section].Count; i++)
                    {
                        _commands[entry.Command.Section][i].Index++;
                    }
                }
                _editor.UpdateTabTitle(false);
            };
            Viewer.DeleteCommand += (o, e) =>
            {
                var treeGridView = (ScriptCommandSectionTreeGridView)o;
                var entry = (ScriptCommandSectionEntry)treeGridView.SelectedItem;
                
                if (entry.Text.StartsWith("NONE") || entry.Text.StartsWith("SCRIPT"))
                {
                    entry.ScriptFile.ScriptSections.Remove(entry.ScriptFile.ScriptSections.First(s => s.Name.Replace("/", "") == entry.Text));
                    LabelsSectionEntry label = entry.ScriptFile.LabelsSection.Objects.FirstOrDefault(l => l.Name.Replace("/", "") == entry.Text);
                    if (label is not null)
                    {
                        entry.ScriptFile.LabelsSection.Objects.Remove(label);
                    }
                    entry.ScriptFile.NumSections--;
                }
                else
                {
                    ScriptCommandInvocation command = entry.Command.Script.ScriptSections[entry.Command.Script.ScriptSections.IndexOf(entry.Command.Section)]
                        .Objects[entry.Command.Index];
                    for (int i = entry.Command.Section.Objects.IndexOf(command); i < entry.Command.Section.Objects.Count; i++)
                    {
                        _commands[entry.Command.Section][i].Index--;
                    }
                    entry.Command.Section.Objects.Remove(command);
                    _commands[entry.Command.Section].Remove(entry.Command);
                }
                _editor.UpdateTabTitle(false);
            };
            Viewer.AddCommand += (o, e) =>
            {
                var treeGridView = (ScriptCommandSectionTreeGridView)o;
                var entry = (ScriptCommandSectionEntry)treeGridView.SelectedItem;

                if (string.IsNullOrEmpty(e.SectionTitle))
                {
                    e.Command.Script.ScriptSections[e.Command.Script.ScriptSections.IndexOf(e.Command.Section)].Objects.Insert(e.Command.Index, e.Command.Invocation);
                    _commands[e.Command.Section].Insert(e.Command.Index, e.Command);
                    for (int i = e.Command.Section.Objects.IndexOf(e.Command.Invocation) + 1; i < e.Command.Section.Objects.Count; i++)
                    {
                        _commands[e.Command.Section][i].Index++;
                    }
                }
                else
                {
                    string sectionName = e.SectionTitle;
                    EventFile scriptFile = entry.ScriptFile is null ? entry.Command.Script : entry.ScriptFile;
                    scriptFile.ScriptSections.Add(new()
                    {
                        Name = sectionName,
                        CommandsAvailable = EventFile.CommandsAvailable,
                        Objects = new(),
                        SectionType = typeof(ScriptSection),
                        ObjectType = typeof(ScriptCommandInvocation),
                    });
                    scriptFile.NumSections++;
                    scriptFile.LabelsSection.Objects.Insert(scriptFile.LabelsSection.Objects.Count - 1, new()
                    {
                        Name = $"NONE/{sectionName[4..]}",
                        Id = (short)(scriptFile.LabelsSection.Objects.Max(l => l.Id) + 1)
                    });
                    _commands.Add(scriptFile.ScriptSections.Last(), new());
                }

                _editor.UpdateTabTitle(false);
            };
        }

        private IEnumerable<ScriptCommandSectionEntry> GetSections()
        {
            foreach (ScriptSection section in Commands.Keys)
            {
                List<ScriptCommandSectionEntry> commands = new();
                foreach (ScriptItemCommand command in Commands[section])
                {
                    commands.Add(new(command));
                }
                ScriptCommandSectionEntry s = new(section, commands, Commands.Values.First().First().Script);
            }
            return Commands.Select(s => new ScriptCommandSectionEntry(s.Key, s.Value.Select(c => new ScriptCommandSectionEntry(c)), Commands.Values.First().First().Script));
        }
    }
}
