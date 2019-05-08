using Music.Core;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GuitarHub
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string[] availableNotes = new[] { "A", "Ab", "A#", "B", "Bb", "C", "C#", "D", "Db", "D#", "E", "Eb", "F", "F#", "G", "Gb", "G#" };

        private Note selectedNote;

        private readonly Style NoteButtonStyle;
        private readonly SolidColorBrush PrimaryBackgroundColorBrush;
        private readonly SolidColorBrush NoteNotInScaleBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));

        private readonly static string StringTag = "FretboardString";

        public MainWindow()
        {
            InitializeComponent();

            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Title = Title.Replace("{version}", version);

            NoteButtonStyle = FindResource("NoteButton") as Style;
            PrimaryBackgroundColorBrush = FindResource("PrimaryBackgroundColorBrush") as SolidColorBrush;

            FillNotes();
            FillScales();
        }

        private void FillNotes()
        {
            foreach (var note in availableNotes)
            {
                var comboItem = new ComboBoxItem
                {
                    Content = note
                };

                Notes.Items.Add(comboItem);
            }

            Notes.SelectedIndex = 0;
        }

        private void FillScales()
        {
            var scaleDefinitions = ScaleEnumerator.ScaleTypes.Select(x => x.Name.Replace("Scale", "")).ToArray();

            foreach (var scale in scaleDefinitions)
            {
                var comboItem = new ComboBoxItem
                {
                    Content = scale
                };

                Scales.Items.Add(comboItem);
            }

            Scales.SelectedIndex = 4;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            selectedNote = MusicNotes.FromString(Notes.SelectionBoxItem as string);
            var scale = CreateScaleInstance(selectedNote);
            var frets = 24;

            ShowScaleDegreeCheckboxes(scale);

            var tuning = new[] { MusicNotes.E, MusicNotes.B, MusicNotes.G, MusicNotes.D, MusicNotes.A, MusicNotes.E };
            var fretboard = new Fretboard(tuning, frets);

            fretboard.SetScale(scale);

            ShowFretboard(selectedNote, scale, frets, fretboard);
        }

        private void ShowScaleDegreeCheckboxes(ScaleBase scale)
        {
            ScaleDegreeSelector.Children.Clear();

            foreach (var item in scale.ChromaticNotes)
            {
                var checkBox = new CheckBox
                {
                    Content = item.Interval.IntervalQuality,
                    Height = 32,
                    IsChecked = item.IsPresent,
                    Margin = new Thickness(5, 20, 5, 0)
                };

                checkBox.Checked += CheckBox_Checked;
                checkBox.Unchecked += CheckBox_Checked;

                ScaleDegreeSelector.Children.Add(checkBox);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = e.Source as CheckBox;
            var isChecked = checkBox.IsChecked ?? false;

            var stringStackPanels = Fretboard.Children
                                             .OfType<StackPanel>()
                                             .Where(x => x.Tag?.ToString() == StringTag)
                                             .ToArray();

            foreach (var stringStackPanel in stringStackPanels)
            {
                var noteButtons = stringStackPanel.Children
                                                  .OfType<Button>()
                                                  .ToArray();

                foreach (var button in noteButtons)
                {
                    var note = button.Content as ScaleNote;
                    var isSelectedInterval = (IntervalQuality)checkBox.Content == note.Interval.IntervalQuality;

                    if (isSelectedInterval)
                    {
                        button.Opacity = isChecked ? note.IsPresent ? 1 : 0.7 : 0;
                    }
                }
            }
        }

        private void ShowFretboard(Note rootNote, ScaleBase scale, int frets, Fretboard fretboard)
        {
            var margin = new Thickness(10, 5, 10, 5);

            Fretboard.Children.Clear();

            ShowFretNumbers(fretboard);
            ShowStringLinesAndFrets(fretboard);

            foreach (var fretNote in fretboard.StringNotes)
            {
                var stringStackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Tag = StringTag
                };

                var openNote = fretNote[0];

                AddNote(openNote, scale, margin, stringStackPanel);

                for (int i = 1; i < fretNote.Length; i++)
                {
                    var note = fretNote[i];

                    AddNote(note, scale, margin, stringStackPanel);
                }

                Fretboard.Children.Add(stringStackPanel);
            }

            ShowFretMarks(fretboard);
        }

        private void ShowFretMarks(Fretboard fretboard)
        {
            var margin = new Thickness(10, 5, 10, 5);
            var fretMarks = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var marks = new[] { 1, 3, 5, 7, 9 };

            for (int i = 0; i < fretboard.NumberOfFrets; i++)
            {
                var mark = string.Empty;
                if (marks.Contains(i % 12))
                {
                    mark = "●";
                }
                else if (i > 0 && i % 12 == 0)
                {
                    mark = "●●";
                }

                AddLabel(margin, fretMarks, mark, 26);
            }

            Fretboard.Children.Add(fretMarks);
        }

        private void AddNote(ScaleNote scaleNote, ScaleBase scale, Thickness margin, StackPanel stringStackPanel)
        {
            var noteButton = new Button
            {
                Style = NoteButtonStyle,
                Margin = margin,
                Content = scaleNote,
                ToolTip = scaleNote.Description
            };

            noteButton.Background = Brushes.DimGray;
            noteButton.BorderBrush = Brushes.Transparent;

            if (!scale.Notes.Any(x => x.Note == scaleNote.Note))
            {
                noteButton.Background = NoteNotInScaleBrush;
                noteButton.Opacity = 0;
            }

            if (scaleNote.Note == selectedNote)
            {
                noteButton.Background = PrimaryBackgroundColorBrush;
            }

            stringStackPanel.Children.Add(noteButton);
        }

        private void ShowFretNumbers(Fretboard fretboard)
        {
            var margin = new Thickness(10, 5, 10, 5);
            var fretNumbers = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            for (int i = 0; i < fretboard.NumberOfFrets; i++)
            {
                AddLabel(margin, fretNumbers, i.ToString());
            }

            Fretboard.Children.Add(fretNumbers);
        }

        private void AddLabel(Thickness margin, StackPanel panel, string content, int fontSize = 12)
        {
            var numberLabel = new Label
            {
                Content = content,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = margin,
                Width = 40,
                FontSize = fontSize
            };

            panel.Children.Add(numberLabel);
        }

        private void ShowStringLinesAndFrets(Fretboard fretboard)
        {
            var x = 60;
            var y = 25;
            var lineStroke = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            var canvas = new Canvas();

            var nutLine = new Line
            {
                Stroke = lineStroke,
                X1 = x,
                X2 = x,
                Y1 = 0,
                Y2 = 300,
                StrokeThickness = 5
            };

            canvas.Children.Add(nutLine);

            foreach (var fretNote in fretboard.StringNotes)
            {
                var line = new Line
                {
                    Stroke = lineStroke,
                    X1 = 0,
                    X2 = Width,
                    Y1 = y,
                    Y2 = y,
                };

                canvas.Children.Add(line);

                y += 50;
            }

            for (int i = 1; i < fretboard.NumberOfFrets; i++)
            {
                x += 60;

                var border = new Rectangle
                {
                    Stroke = lineStroke,
                    Fill = Brushes.Transparent,
                    Width = 5,
                    Height = 300,
                    Margin = new Thickness(x - 3, 0, 0, 0)
                };

                canvas.Children.Add(border);


            }

            Fretboard.Children.Add(canvas);
        }

        private ScaleBase CreateScaleInstance(Note rootNote)
        {
            var scale = Scales.SelectionBoxItem as string;
            var scaleType = ScaleEnumerator.ScaleTypes.First(x => x.Name.ToLowerInvariant().StartsWith(scale.ToLowerInvariant()));

            return (ScaleBase)Activator.CreateInstance(scaleType, rootNote);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
