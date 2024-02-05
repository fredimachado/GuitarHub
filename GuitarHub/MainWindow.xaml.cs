using GuitarHub.Properties;
using Music.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace GuitarHub
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string[] availableNotes = new[] { "A", "Ab", "A#", "B", "Bb", "C", "C#", "D", "Db", "D#", "E", "Eb", "F", "F#", "G", "Gb", "G#" };

        private readonly static Note[] standardTuning = new[] { MusicNotes.E, MusicNotes.B, MusicNotes.G, MusicNotes.D, MusicNotes.A, MusicNotes.E };
        private readonly static Note[] standardTuningFlip = standardTuning.Reverse().ToArray();

        private Note selectedNote;
        private ScaleBase currentScale;

        private readonly Style NoteButtonStyle;
        private readonly SolidColorBrush PrimaryBackgroundColorBrush;
        private readonly SolidColorBrush NoteNotInScaleBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));

        private readonly static string StringTag = "FretboardString";

        private int currentFretLowValue = 0;
        private int currentFretHighValue = 24;

        private readonly static Transform invertedTransform = new ScaleTransform
        {
            ScaleX = -1
        };

        public MainWindow()
        {
            InitializeComponent();

            var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            Title = Title.Replace("{version}", $"{versionInfo.FileMajorPart}.{versionInfo.FileMinorPart}.{versionInfo.FileBuildPart}");

            NoteButtonStyle = FindResource("NoteButton") as Style;
            PrimaryBackgroundColorBrush = FindResource("PrimaryBackgroundColorBrush") as SolidColorBrush;

            FillNotes();
            FillScales();

            LoadSettings();

            LeftHanded.Checked += (object sender, RoutedEventArgs e) => OkButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            LeftHanded.Unchecked += (object sender, RoutedEventArgs e) => OkButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            FlipNut.Checked += (object sender, RoutedEventArgs e) => OkButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            FlipNut.Unchecked += (object sender, RoutedEventArgs e) => OkButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
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
            var scaleDefinitions = ScaleEnumerator.ScaleTypes.Select(x => x.Name.Replace("Scale", ""));

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
            currentScale = CreateScaleInstance(selectedNote);
            var frets = 24;

            ShowScaleDegreeCheckboxes(currentScale);

            var tuning = FlipNut.IsChecked.Value ? standardTuningFlip : standardTuning;
            var fretboard = new Fretboard(tuning, frets);

            fretboard.SetScale(currentScale);

            ShowFretboard(selectedNote, currentScale, frets, fretboard);

            FretRange.RenderTransformOrigin = new Point(0.5, 0.5);
            FretRange.RenderTransform = LeftHanded.IsChecked.Value ? invertedTransform : Transform.Identity;

            (IntervalFilter.Items[1] as ComboBoxItem).IsEnabled = currentScale.Notes.Length == 7;
            (IntervalFilter.Items[2] as ComboBoxItem).IsEnabled = currentScale.Notes.Length == 7;
            (IntervalFilter.Items[6] as ComboBoxItem).IsEnabled = currentScale.Notes.Length == 7;
            (IntervalFilter.Items[7] as ComboBoxItem).IsEnabled = currentScale.Notes.Length == 7;

            (IntervalFrom.Items[5] as ComboBoxItem).IsEnabled = currentScale.Notes.Length == 7;
            (IntervalFrom.Items[6] as ComboBoxItem).IsEnabled = currentScale.Notes.Length == 7;

            FilterButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            SaveSettings();
        }

        private void ShowNoteInterval_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = e.Source as CheckBox;
            var isChecked = checkBox.IsChecked.Value;

            AffectNoteButton(ShowNoteInterval(isChecked));
        }

        private void ShowScaleDegreeCheckboxes(ScaleBase scale)
        {
            DegreeSelector.Children.Clear();

            DegreeSelector.Children.Add(new Label { Content = "Scale Degrees:", Height = 32, VerticalContentAlignment = VerticalAlignment.Center });

            foreach (var item in scale.ChromaticNotes)
            {
                var checkBox = new CheckBox
                {
                    Content = item.Interval.IntervalQuality,
                    Height = 32,
                    IsChecked = item.IsPresent,
                    Margin = new Thickness(5, 0, 5, 0),
                    ToolTip = item.Interval.ToString(),
                    VerticalContentAlignment = VerticalAlignment.Center
                };

                checkBox.Checked += ScaleDegree_Checked;
                checkBox.Unchecked += ScaleDegree_Checked;

                DegreeSelector.Children.Add(checkBox);
            }
        }

        private void ScaleDegree_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = e.Source as CheckBox;
            var isChecked = checkBox.IsChecked.Value;

            AffectNoteButton(ShowHideScaleDegrees(checkBox, isChecked));
        }

        private void AffectNoteButton(Action<Button> action)
        {
            var stringStackPanels = Fretboard.Children
                                             .OfType<StackPanel>()
                                             .Where(x => ReferenceEquals(x.Tag, StringTag));

            foreach (var stringStackPanel in stringStackPanels)
            {
                var noteButtons = stringStackPanel.Children
                                                  .OfType<Button>();

                foreach (var button in noteButtons)
                {
                    action(button);
                }
            }
        }

        private void ShowFretboard(Note rootNote, ScaleBase scale, int frets, Fretboard fretboard)
        {
            var margin = new Thickness(10, 5, 10, 5);

            Fretboard.Children.Clear();

            ShowFretNumbers(fretboard);
            ShowStringLinesAndFrets(fretboard);

            for (int i = 0; i < fretboard.StringNotes.Count; i++)
            {
                var stringNotes = fretboard.StringNotes[i];
                var stringStackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    FlowDirection = LeftHanded.IsChecked.Value ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                    Tag = StringTag
                };

                for (int j = 0; j < stringNotes.Length; j++)
                {
                    var note = stringNotes[j];

                    AddNote(note, scale, margin, stringNumber: i, fretNumber: j, stringStackPanel);
                }

                Fretboard.Children.Add(stringStackPanel);

                if ((!(String1.IsChecked ?? false) && i == 0)
                    || (!(String2.IsChecked ?? false) && i == 1)
                    || (!(String3.IsChecked ?? false) && i == 2)
                    || (!(String4.IsChecked ?? false) && i == 3)
                    || (!(String5.IsChecked ?? false) && i == 4)
                    || (!(String6.IsChecked ?? false) && i == 5))
                {
                    stringStackPanel.Visibility = Visibility.Hidden;
                }
            }

            ShowFretMarks(fretboard);
        }

        private void ShowFretMarks(Fretboard fretboard)
        {
            var margin = new Thickness(10, 5, 10, 5);
            var fretMarks = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                FlowDirection = LeftHanded.IsChecked.Value ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
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

        private void ShowFretNumbers(Fretboard fretboard)
        {
            var margin = new Thickness(10, 0, 10, 0);
            var fretNumbers = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                FlowDirection = LeftHanded.IsChecked.Value ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
            };

            for (int i = 0; i < fretboard.NumberOfFrets; i++)
            {
                AddLabel(margin, fretNumbers, i.ToString(), uid: $"fret{i}");
            }

            Fretboard.Children.Add(fretNumbers);
        }

        private void AddNote(ScaleNote scaleNote, ScaleBase scale, Thickness margin, int stringNumber, int fretNumber, StackPanel stringStackPanel)
        {
            var fretNote = new FretNote(scaleNote, stringNumber, fretNumber);
            var noteButton = new Button
            {
                Style = NoteButtonStyle,
                Margin = margin,
                Content = scaleNote.ToString(),
                Tag = fretNote,
                ToolTip = scaleNote.Description
            };

            noteButton.Background = Brushes.DimGray;
            noteButton.BorderBrush = Brushes.Transparent;

            if (!scale.Notes.Any(x => x.Note == scaleNote.Note))
            {
                noteButton.Background = NoteNotInScaleBrush;
                noteButton.Opacity = 0;
            }

            if (!IsFretNoteVisible(fretNote))
            {
                noteButton.Opacity = 0;
            }

            if (scaleNote.Note == selectedNote)
            {
                noteButton.Background = PrimaryBackgroundColorBrush;
            }

            stringStackPanel.Children.Add(noteButton);
        }

        private void RefreshFretNotes(object sender, RoutedEventArgs e)
        {
            var rangeSlider = e.Source as RangeSlider;

            var lowerValue = (int)(rangeSlider.LowerValue + 0.5);
            var higherValue = (int)(rangeSlider.HigherValue + 0.5);

            if (lowerValue == currentFretLowValue && higherValue == currentFretHighValue)
            {
                return;
            }

            currentFretLowValue = lowerValue;
            currentFretHighValue = higherValue;

            AffectNoteButton(RefreshFretNotes());

            FilterButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        private bool IsFretNoteVisible(FretNote fretNote)
        {
            return fretNote.FretNumber >= currentFretLowValue && fretNote.FretNumber <= currentFretHighValue;
        }

        private void AddLabel(Thickness margin, StackPanel panel, string content, int fontSize = 12, string uid = null)
        {
            var label = new Label
            {
                Content = content,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = margin,
                Width = 40,
                FontSize = fontSize
            };

            if (uid != null)
            {
                label.Uid = uid;
            }

            panel.Children.Add(label);
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

            if (LeftHanded.IsChecked.Value)
            {
                canvas.RenderTransformOrigin = new Point(0.5, 0.5);
                canvas.RenderTransform = invertedTransform;
            }
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

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentScale == null ||
                !((IntervalFilter.SelectedItem as ComboBoxItem)?.Content is string intervalFilter) ||
                intervalFilter == string.Empty ||
                IntervalFrom.SelectedIndex < 0)
            {
                return;
            }

            var scaleNotes = Rotate(currentScale.Notes, IntervalFrom.SelectedIndex);

            var intervalNotes = new List<ScaleNote>
            {
                scaleNotes.First()
            };

            if (intervalFilter == "Triad")
            {
                intervalNotes.Add(scaleNotes.Skip(2).First());
                intervalNotes.Add(scaleNotes.Skip(4).First());
            }
            else if (intervalFilter == "Tetrad (7th)")
            {
                intervalNotes.Add(scaleNotes.Skip(2).First());
                intervalNotes.Add(scaleNotes.Skip(4).First());
                intervalNotes.Add(scaleNotes.Skip(6).First());
            }
            else if (intervalFilter == "Third")
            {
                intervalNotes.Add(scaleNotes.Skip(2).First());
            }
            else if (intervalFilter == "Forth")
            {
                intervalNotes.Add(scaleNotes.Skip(3).First());
            }
            else if (intervalFilter == "Fifth")
            {
                intervalNotes.Add(scaleNotes.Skip(4).First());
            }
            else if (intervalFilter == "Sixth")
            {
                intervalNotes.Add(scaleNotes.Skip(5).First());
            }
            else if (intervalFilter == "Seventh")
            {
                intervalNotes.Add(scaleNotes.Skip(6).First());
            }

            AffectNoteButton(FilterIntervals(intervalNotes));
        }

        private static IEnumerable<ScaleNote> Rotate(IEnumerable<ScaleNote> notes, int fromIndex)
        {
            if (fromIndex == 0)
            {
                return notes;
            }

            var newEnd = notes.Take(fromIndex);
            var newStart = notes.Skip(fromIndex);

            return newStart.Concat(newEnd);
        }

        private static Action<Button> ShowNoteInterval(bool isChecked)
        {
            return button =>
            {
                var fretNote = button.Tag as FretNote;
                button.Content = isChecked ? fretNote.Note.Interval.IntervalQuality.ToString() : fretNote.Note.ToString();
            };
        }

        private Action<Button> ShowHideScaleDegrees(CheckBox checkBox, bool isChecked)
        {
            return button =>
            {
                var fretNote = button.Tag as FretNote;
                var isSelectedInterval = (IntervalQuality)checkBox.Content == fretNote.Note.Interval.IntervalQuality;

                if (isSelectedInterval && IsFretNoteVisible(fretNote))
                {
                    button.Opacity = isChecked && IsFretNoteVisible(fretNote) ? fretNote.Note.IsPresent ? 1 : 0.5 : 0;
                }
            };
        }

        private Action<Button> RefreshFretNotes()
        {
            return button =>
            {
                var fretNote = button.Tag as FretNote;
                button.Opacity = IsFretNoteVisible(fretNote) && fretNote.Note.IsPresent ? 1 : 0;
            };
        }

        private Action<Button> FilterIntervals(List<ScaleNote> intervalNotes)
        {
            return button =>
            {
                var fretNote = button.Tag as FretNote;

                if (intervalNotes.Any(x => x.ScaleDegree == fretNote.Note.ScaleDegree))
                {
                    button.Opacity = 1;
                }
                else
                {
                    button.Opacity = !HideScale.IsChecked.Value && fretNote.Note.IsPresent ? 0.3 : 0;
                }

                if (!IsFretNoteVisible(fretNote))
                {
                    button.Opacity = 0;
                }
            };
        }

        private void SaveSettings()
        {
            Settings.Default.Note = Notes.SelectionBoxItem as string;
            Settings.Default.Scale = Scales.SelectionBoxItem as string;
            Settings.Default.LeftHanded = LeftHanded.IsChecked.Value;
            Settings.Default.FlipNut = FlipNut.IsChecked.Value;
            Settings.Default.Save();
        }

        private void LoadSettings()
        {
            Notes.SelectedIndex = Notes.Items.IndexOf(Notes.Items.OfType<ComboBoxItem>().FirstOrDefault(x => x.Content as string == Settings.Default.Note));
            Scales.SelectedIndex = Scales.Items.IndexOf(Scales.Items.OfType<ComboBoxItem>().FirstOrDefault(x => x.Content as string == Settings.Default.Scale));
            LeftHanded.IsChecked = Settings.Default.LeftHanded;
            FlipNut.IsChecked = Settings.Default.FlipNut;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }
    }

    public class FretNote
    {
        public FretNote(ScaleNote note, int stringNumber, int fretNumber)
        {
            Note = note;
            StringNumber = stringNumber;
            FretNumber = fretNumber;
        }

        public ScaleNote Note { get; }
        public int StringNumber { get; }
        public int FretNumber { get; }
    }
}
