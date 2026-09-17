using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace OneMenu
{
    public class WelcomeStep
    {
        public int Number { get; set; }
        public string Text { get; set; }
    }

    public class WelcomePage
    {
        public string Glyph { get; set; }
        public string TitleKey { get; set; }
        public string IntroKey { get; set; }
        public string[] StepKeys { get; set; } = new string[0];
        public bool ShowBugReport { get; set; }
        public bool UseMainIcon { get; set; }
    }

    public partial class WelcomeWindow : Window
    {
        private const string DiscordUrl = "https://discord.gg/BrtABqe";

        private static readonly ILogger logger = LogManager.GetLogger();
        private static readonly Brush inactiveDotBrush = CreateInactiveDotBrush();

        private readonly List<WelcomePage> pages;
        private readonly List<Ellipse> dots = new List<Ellipse>();
        private int currentIndex;

        public WelcomeWindow()
        {
            InitializeComponent();
            OneMenuTheme.Apply(this, !OneMenuPlugin.FollowPlayniteTheme);

            pages = CreatePages();
            BuildDots();
            ShowPage(0, true, false);
        }

        private static Brush CreateInactiveDotBrush()
        {
            var brush = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF));
            brush.Freeze();
            return brush;
        }

        private static List<WelcomePage> CreatePages()
        {
            return new List<WelcomePage>
            {
                new WelcomePage
                {
                    Glyph = "\xE7FC",
                    UseMainIcon = true,
                    TitleKey = "LOCOneMenuWelcomeP1Title",
                    IntroKey = "LOCOneMenuWelcomeP1Intro",
                    StepKeys = new[] { "LOCOneMenuWelcomeP1Step1", "LOCOneMenuWelcomeP1Step2", "LOCOneMenuWelcomeP1Step3" }
                },
                new WelcomePage
                {
                    Glyph = "\xE713",
                    TitleKey = "LOCOneMenuWelcomeP2Title",
                    IntroKey = "LOCOneMenuWelcomeP2Intro",
                    StepKeys = new[] { "LOCOneMenuWelcomeP2Step1", "LOCOneMenuWelcomeP2Step2", "LOCOneMenuWelcomeP2Step3" }
                },
                new WelcomePage
                {
                    Glyph = "\xE8FD",
                    TitleKey = "LOCOneMenuWelcomeP3Title",
                    IntroKey = "LOCOneMenuWelcomeP3Intro",
                    StepKeys = new[] { "LOCOneMenuWelcomeP3Step1", "LOCOneMenuWelcomeP3Step2", "LOCOneMenuWelcomeP3Step3", "LOCOneMenuWelcomeP3Step4" }
                },
                new WelcomePage
                {
                    Glyph = "\xE71C",
                    TitleKey = "LOCOneMenuWelcomeP4Title",
                    IntroKey = "LOCOneMenuWelcomeP4Intro",
                    StepKeys = new[] { "LOCOneMenuWelcomeP4Step1", "LOCOneMenuWelcomeP4Step2", "LOCOneMenuWelcomeP4Step3" }
                },
                new WelcomePage
                {
                    Glyph = "\xE8B9",
                    TitleKey = "LOCOneMenuWelcomeP5Title",
                    IntroKey = "LOCOneMenuWelcomeP5Intro",
                    StepKeys = new[] { "LOCOneMenuWelcomeP5Step1", "LOCOneMenuWelcomeP5Step2", "LOCOneMenuWelcomeP5Step3", "LOCOneMenuWelcomeP5Step4" }
                },
                new WelcomePage
                {
                    Glyph = "\xE90C",
                    TitleKey = "LOCOneMenuWelcomeSideTitle",
                    IntroKey = "LOCOneMenuWelcomeSideIntro",
                    StepKeys = new[] { "LOCOneMenuWelcomeSideStep1", "LOCOneMenuWelcomeSideStep2", "LOCOneMenuWelcomeSideStep3" }
                },
                new WelcomePage
                {
                    Glyph = "\xE721",
                    TitleKey = "LOCOneMenuWelcomeP6Title",
                    IntroKey = "LOCOneMenuWelcomeP6Intro",
                    StepKeys = new[] { "LOCOneMenuWelcomeP6Step1", "LOCOneMenuWelcomeP6Step2", "LOCOneMenuWelcomeP6Step3", "LOCOneMenuWelcomeP6Step4" }
                },
                new WelcomePage
                {
                    Glyph = "\xE8EC",
                    TitleKey = "LOCOneMenuWelcomeP7Title",
                    IntroKey = "LOCOneMenuWelcomeP7Intro",
                    StepKeys = new[] { "LOCOneMenuWelcomeP7Step1", "LOCOneMenuWelcomeP7Step2", "LOCOneMenuWelcomeP7Step3" }
                },
                new WelcomePage
                {
                    Glyph = "\xE771",
                    TitleKey = "LOCOneMenuWelcomeP8Title",
                    IntroKey = "LOCOneMenuWelcomeP8Intro",
                    StepKeys = new[] { "LOCOneMenuWelcomeP8Step1", "LOCOneMenuWelcomeP8Step2", "LOCOneMenuWelcomeP8Step3" }
                },
                new WelcomePage
                {
                    Glyph = "\xE73E",
                    TitleKey = "LOCOneMenuWelcomeP9Title",
                    IntroKey = "LOCOneMenuWelcomeP9Intro",
                    ShowBugReport = true
                }
            };
        }

        private void BuildDots()
        {
            for (var i = 0; i < pages.Count; i++)
            {
                var pageIndex = i;
                var dot = new Ellipse
                {
                    Width = 9,
                    Height = 9,
                    Margin = new Thickness(4, 0, 4, 0),
                    Cursor = Cursors.Hand,
                    Fill = inactiveDotBrush
                };
                dot.MouseLeftButtonUp += (s, e) => ShowPage(pageIndex, pageIndex >= currentIndex, true);

                dots.Add(dot);
                DotsPanel.Children.Add(dot);
            }
        }

        private void ShowPage(int index, bool forward, bool animate)
        {
            if (index < 0 || index >= pages.Count)
            {
                return;
            }

            currentIndex = index;
            var page = pages[index];
            var isFirst = index == 0;
            var isLast = index == pages.Count - 1;

            var mainIcon = page.UseMainIcon ? ImageHelper.LoadFromFile(OneMenuPlugin.DefaultIconFile, 192) : null;
            PageImage.Source = mainIcon;
            PageImage.Visibility = mainIcon != null ? Visibility.Visible : Visibility.Collapsed;
            PageGlyphCircle.Visibility = mainIcon != null ? Visibility.Collapsed : Visibility.Visible;
            PageGlyph.Text = page.Glyph;
            PageTitle.Text = Loc.Get(page.TitleKey);
            PageIntro.Text = Loc.Get(page.IntroKey);

            StepsList.ItemsSource = page.StepKeys
                .Select((key, position) => new WelcomeStep { Number = position + 1, Text = Loc.Get(key) })
                .ToList();
            StepsList.Visibility = page.StepKeys.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

            BugReportPanel.Visibility = page.ShowBugReport ? Visibility.Visible : Visibility.Collapsed;

            BackButton.Visibility = isFirst ? Visibility.Hidden : Visibility.Visible;
            NextButton.Visibility = isLast ? Visibility.Hidden : Visibility.Visible;
            SkipButton.Visibility = isLast ? Visibility.Collapsed : Visibility.Visible;
            FinishButton.Visibility = isLast ? Visibility.Visible : Visibility.Collapsed;
            PageCounterText.Text = Loc.Format("LOCOneMenuWelcomePageOf", index + 1, pages.Count);

            for (var i = 0; i < dots.Count; i++)
            {
                if (i == index)
                {
                    dots[i].SetResourceReference(Shape.FillProperty, "GlyphBrush");
                }
                else
                {
                    dots[i].Fill = inactiveDotBrush;
                }
            }

            if (animate)
            {
                var duration = TimeSpan.FromMilliseconds(240);
                var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };

                PageContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration));
                PageTranslate.BeginAnimation(
                    TranslateTransform.XProperty,
                    new DoubleAnimation(forward ? 48 : -48, 0, duration) { EasingFunction = ease });
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(currentIndex + 1, true, true);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(currentIndex - 1, false, true);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right || e.Key == Key.PageDown)
            {
                ShowPage(currentIndex + 1, true, true);
                e.Handled = true;
            }
            else if (e.Key == Key.Left || e.Key == Key.PageUp)
            {
                ShowPage(currentIndex - 1, false, true);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(DiscordUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "OneMenu: couldn't open the Playnite Discord link.");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
